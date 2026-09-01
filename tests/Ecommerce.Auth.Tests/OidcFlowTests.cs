using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class OidcFlowTests(AuthApiFactory factory)
{
    private const string AdminEmail = "admin@ecom.local";
    private const string AdminPassword = "Admin@123456";

    [Fact]
    public async Task Discovery_document_advertises_the_supported_grants()
    {
        using var json = JsonDocument.Parse(await factory.CreateClient().GetStringAsync("/.well-known/openid-configuration"));
        var root = json.RootElement;
        Assert.Contains("/connect/token", root.GetProperty("token_endpoint").GetString()!);
        var grants = root.GetProperty("grant_types_supported").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("password", grants);
        Assert.Contains("refresh_token", grants);
        Assert.Contains("client_credentials", grants);
    }

    [Fact]
    public async Task Jwks_exposes_only_public_signing_material()
    {
        using var json = JsonDocument.Parse(
            await factory.CreateClient().GetStringAsync("/.well-known/jwks"));

        var keys = json.RootElement.GetProperty("keys").EnumerateArray().ToList();
        Assert.NotEmpty(keys);

        foreach (var key in keys)
        {
            Assert.Equal("RSA", key.GetProperty("kty").GetString());

            Assert.True(key.TryGetProperty("n", out _));
            Assert.True(key.TryGetProperty("e", out _));
            Assert.False(key.TryGetProperty("d", out _), "JWKS không được chứa private exponent.");
            Assert.False(key.TryGetProperty("p", out _), "JWKS không được chứa prime factor.");
            Assert.False(key.TryGetProperty("q", out _), "JWKS không được chứa prime factor.");
        }
    }

    [Fact]
    public async Task Password_grant_returns_access_and_refresh_token()
    {
        var tokens = await factory.CreateClient().SignInAsync(AdminEmail, AdminPassword);

        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
        Assert.False(string.IsNullOrEmpty(tokens.IdToken));
    }

    [Fact]
    public async Task Password_grant_with_wrong_password_returns_invalid_grant()
    {
        var resp = await factory.CreateClient().PasswordGrantAsync(AdminEmail, "SaiMatKhau!");

        Assert.False(resp.IsSuccessStatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(resp));
        Assert.Equal("Invalid email or password.", await OidcFlowHelper.ReadErrorDescriptionAsync(resp));
    }

    [Fact]
    public async Task Password_grant_with_unknown_email_gives_the_same_error()
    {
        var resp = await factory.CreateClient()
            .PasswordGrantAsync($"khong-ton-tai-{Guid.CreateVersion7():N}@ecom.test", "Passw0rd!");

        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(resp));
        Assert.Equal("Invalid email or password.", await OidcFlowHelper.ReadErrorDescriptionAsync(resp));
    }

    [Fact]
    public async Task Access_token_is_a_readable_rs256_jwt_with_the_required_claims()
    {
        var tokens = await factory.CreateClient().SignInAsync(AdminEmail, AdminPassword);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);

        Assert.Equal("RS256", jwt.Alg);

        Assert.False(string.IsNullOrEmpty(jwt.Subject));
        Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == AdminEmail);
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type == "jti");

        var scope = jwt.Claims.Single(c => c.Type == "scope").Value.Split(' ');
        Assert.Contains("product.write", scope);

        Assert.Contains(jwt.Claims, c => c.Type == "aud" && c.Value == "product-api");
    }

    [Fact]
    public async Task Access_token_is_accepted_by_the_me_endpoint()
    {
        var client = factory.CreateClient();
        var tokens = await client.SignInAsync(AdminEmail, AdminPassword);

        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var resp = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        Assert.Equal(AdminEmail, body!.Data!.Email);
    }

    [Fact]
    public async Task Refresh_token_rotation_invalidates_the_previous_token()
    {
        var client = factory.CreateClient();
        var initial = await client.SignInAsync(AdminEmail, AdminPassword);

        var refreshed = await client.RefreshAsync(initial.RefreshToken!);
        refreshed.EnsureSuccessStatusCode();
        var rotated = await OidcFlowHelper.ReadTokensAsync(refreshed);

        Assert.NotEqual(initial.RefreshToken, rotated.RefreshToken);

        var reuse = await client.RefreshAsync(initial.RefreshToken!);

        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(reuse));
    }

    [Fact]
    public async Task Revoked_refresh_token_cannot_be_used_again()
    {
        var client = factory.CreateClient();
        var tokens = await client.SignInAsync(AdminEmail, AdminPassword);

        var revoke = await client.RevokeAsync(tokens.RefreshToken!);
        revoke.EnsureSuccessStatusCode();

        var afterRevoke = await client.RefreshAsync(tokens.RefreshToken!);

        Assert.Equal(HttpStatusCode.BadRequest, afterRevoke.StatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(afterRevoke));
    }

    [Fact]
    public async Task Refresh_token_is_not_issued_without_offline_access_scope()
    {
        var tokens = await factory.CreateClient()
            .SignInAsync(AdminEmail, AdminPassword, scope: "openid product.read");

        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
        Assert.Null(tokens.RefreshToken);
    }

    [Fact]
    public async Task Client_credentials_grant_returns_a_service_token()
    {
        var resp = await factory.CreateClient().PostTokenAsync(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "service-worker",
                ["client_secret"] = "service_worker_dev_secret",
                ["scope"] = "product.read"
            });

        resp.EnsureSuccessStatusCode();
        var tokens = await OidcFlowHelper.ReadTokensAsync(resp);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);
        Assert.Equal("service-worker", jwt.Subject);
        Assert.Contains("product.read", jwt.Claims.Single(c => c.Type == "scope").Value.Split(' '));

        Assert.Null(tokens.RefreshToken);
    }

    [Fact]
    public async Task Client_credentials_with_wrong_secret_is_rejected()
    {
        var resp = await factory.CreateClient().PostTokenAsync(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "service-worker",
                ["client_secret"] = "wrong-secret",
                ["scope"] = "product.read"
            });

        Assert.False(resp.IsSuccessStatusCode);
        Assert.Equal("invalid_client", await OidcFlowHelper.ReadErrorAsync(resp));
    }

    [Fact]
    public async Task Scope_not_granted_to_the_client_is_rejected()
    {
        var resp = await factory.CreateClient().PostTokenAsync(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = "service-worker",
                ["client_secret"] = "service_worker_dev_secret",
                ["scope"] = "product.write"
            });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
