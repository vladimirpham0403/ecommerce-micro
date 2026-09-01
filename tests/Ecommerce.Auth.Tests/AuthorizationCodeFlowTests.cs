using System.Net;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class AuthorizationCodeFlowTests(AuthApiFactory factory)
{
    private const string AdminEmail = "admin@ecom.local";
    private const string AdminPassword = "Admin@123456";

    [Fact]
    public async Task Discovery_advertises_authorization_code_with_pkce()
    {
        using var json = System.Text.Json.JsonDocument.Parse(
            await factory.CreateClient().GetStringAsync("/.well-known/openid-configuration"));
        var root = json.RootElement;

        Assert.Contains("/connect/authorize", root.GetProperty("authorization_endpoint").GetString()!);

        var grants = root.GetProperty("grant_types_supported")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("authorization_code", grants);

        var methods = root.GetProperty("code_challenge_methods_supported")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("S256", methods);
    }

    [Fact]
    public async Task Full_flow_returns_access_and_refresh_token()
    {
        var client = factory.CreateFlowClient();

        var tokens = await factory.SignInWithAuthorizationCodeAsync(client, AdminEmail, AdminPassword);

        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.False(string.IsNullOrEmpty(tokens.IdToken));

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);
        Assert.Equal("RS256", jwt.Alg);
        Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == AdminEmail);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task Authorize_without_pkce_is_rejected()
    {
        var client = factory.CreateFlowClient();

        var resp = await client.GetAsync(
            $"/connect/authorize?client_id={OidcFlowHelper.ClientId}&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(AuthApiFactory.RedirectUri)}&scope=openid");

        Assert.NotEqual(HttpStatusCode.Redirect, resp.StatusCode);
    }

    [Fact]
    public async Task Authorize_with_unregistered_redirect_uri_is_rejected()
    {
        var (_, challenge) = OidcFlowHelper.CreatePkcePair();
        var client = factory.CreateFlowClient();

        var resp = await client.GetAsync(
            $"/connect/authorize?client_id={OidcFlowHelper.ClientId}&response_type=code" +
            "&redirect_uri=https%3A%2F%2Fke-tan-cong.example%2Fcallback&scope=openid" +
            $"&code_challenge={challenge}&code_challenge_method=S256");

        Assert.NotEqual(HttpStatusCode.Redirect, resp.StatusCode);
    }

    [Fact]
    public async Task Code_cannot_be_exchanged_with_a_wrong_verifier()
    {
        var client = factory.CreateFlowClient();
        var (code, _) = await factory.AuthorizeAsync(client, AdminEmail, AdminPassword);

        var (otherVerifier, _) = OidcFlowHelper.CreatePkcePair();

        var resp = await client.PostTokenAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OidcFlowHelper.ClientId,
            ["redirect_uri"] = AuthApiFactory.RedirectUri,
            ["code"] = code,
            ["code_verifier"] = otherVerifier
        });

        Assert.False(resp.IsSuccessStatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(resp));
    }

    [Fact]
    public async Task Code_cannot_be_used_twice()
    {
        var client = factory.CreateFlowClient();
        var (code, verifier) = await factory.AuthorizeAsync(client, AdminEmail, AdminPassword);

        var first = await client.ExchangeCodeAsync(code, verifier);
        Assert.False(string.IsNullOrEmpty(first.AccessToken));

        var second = await client.PostTokenAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = OidcFlowHelper.ClientId,
            ["redirect_uri"] = AuthApiFactory.RedirectUri,
            ["code"] = code,
            ["code_verifier"] = verifier
        });

        Assert.False(second.IsSuccessStatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(second));
    }

    [Fact]
    public async Task Wrong_password_keeps_the_user_on_the_login_page()
    {
        var client = factory.CreateFlowClient();

        var challengeResponse = await client.GetAsync(
            $"/connect/authorize?client_id={OidcFlowHelper.ClientId}&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(AuthApiFactory.RedirectUri)}&scope=openid" +
            $"&code_challenge={OidcFlowHelper.CreatePkcePair().Challenge}&code_challenge_method=S256");

        var loginUrl = challengeResponse.Headers.Location!.ToString();
        var resp = await client.SubmitLoginFormAsync(loginUrl, AdminEmail, "SaiMatKhau!");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("Invalid email or password", await resp.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }
}
