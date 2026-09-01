using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using Ecommerce.Auth.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ecommerce.Auth.Tests;

public sealed record TokenSet(string AccessToken, string? RefreshToken, string? IdToken);

public static partial class OidcFlowHelper
{
    public const string ClientId = "web-client";
    public const string DefaultScope = "openid email profile roles offline_access product.read product.write";

    public const string AdminScope = DefaultScope + " " + ScopeNames.UsersManage;

    public static HttpClient CreateFlowClient(this AuthApiFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    public static (string Verifier, string Challenge) CreatePkcePair()
    {
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    public static async Task<TokenSet> SignInWithAuthorizationCodeAsync(
        this AuthApiFactory factory,
        HttpClient client,
        string email,
        string password,
        string scope = DefaultScope)
    {
        var (code, verifier) = await factory.AuthorizeAsync(client, email, password, scope);
        return await client.ExchangeCodeAsync(code, verifier);
    }

    public static async Task<(string Code, string Verifier)> AuthorizeAsync(
        this AuthApiFactory factory,
        HttpClient client,
        string email,
        string password,
        string scope = DefaultScope)
    {
        var (verifier, challenge) = CreatePkcePair();

        var authorizeUrl = "/connect/authorize" +
                           $"?client_id={ClientId}" +
                           "&response_type=code" +
                           $"&redirect_uri={Uri.EscapeDataString(AuthApiFactory.RedirectUri)}" +
                           $"&scope={Uri.EscapeDataString(scope)}" +
                           $"&code_challenge={challenge}" +
                           "&code_challenge_method=S256";

        var challengeResponse = await client.GetAsync(authorizeUrl);
        Assert.True(
            challengeResponse.StatusCode == HttpStatusCode.Redirect,
            $"authorize trả {(int)challengeResponse.StatusCode}: {await challengeResponse.Content.ReadAsStringAsync()}");

        var loginUrl = challengeResponse.Headers.Location!.ToString();
        Assert.Contains("/Account/Login", loginUrl, StringComparison.OrdinalIgnoreCase);

        await client.SubmitLoginFormAsync(loginUrl, email, password);

        var codeResponse = await client.GetAsync(authorizeUrl);
        Assert.Equal(HttpStatusCode.Redirect, codeResponse.StatusCode);

        var location = codeResponse.Headers.Location!.ToString();
        Assert.StartsWith(AuthApiFactory.RedirectUri, location, StringComparison.Ordinal);

        var code = HttpUtility.ParseQueryString(new Uri(location).Query)["code"];
        Assert.False(string.IsNullOrEmpty(code), $"Không tìm thấy authorization code trong '{location}'.");

        return (code!, verifier);
    }

    public static async Task<HttpResponseMessage> SubmitLoginFormAsync(
        this HttpClient client, string loginUrl, string email, string password)
    {
        var page = await client.GetAsync(loginUrl);
        page.EnsureSuccessStatusCode();

        var html = await page.Content.ReadAsStringAsync();
        var token = AntiforgeryRegex().Match(html);
        Assert.True(token.Success, "Không tìm thấy __RequestVerificationToken trong trang login.");

        return await client.PostAsync(loginUrl, new FormUrlEncodedContent(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token.Groups[1].Value
            }));
    }

    public static async Task<TokenSet> ExchangeCodeAsync(this HttpClient client, string code, string verifier)
    {
        var response = await client.PostTokenAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = AuthApiFactory.RedirectUri,
            ["code"] = code,
            ["code_verifier"] = verifier
        });

        response.EnsureSuccessStatusCode();
        return await ReadTokensAsync(response);
    }

    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""")]
    private static partial Regex AntiforgeryRegex();

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static async Task<TokenSet> SignInAsync(
        this HttpClient client,
        string email,
        string password,
        string scope = DefaultScope)
    {
        var response = await client.PasswordGrantAsync(email, password, scope);
        response.EnsureSuccessStatusCode();
        return await ReadTokensAsync(response);
    }

    public static Task<HttpResponseMessage> PasswordGrantAsync(
        this HttpClient client,
        string email,
        string password,
        string scope = DefaultScope) =>
        client.PostTokenAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["client_id"] = ClientId,
            ["username"] = email,
            ["password"] = password,
            ["scope"] = scope
        });

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client, string refreshToken) =>
        client.PostTokenAsync(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = ClientId,
            ["refresh_token"] = refreshToken
        });

    public static Task<HttpResponseMessage> RevokeAsync(this HttpClient client, string token) =>
        client.PostAsync("/connect/revoke", new FormUrlEncodedContent(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["client_id"] = ClientId,
                ["token"] = token,
                ["token_type_hint"] = "refresh_token"
            }));

    public static Task<HttpResponseMessage> PostTokenAsync(
        this HttpClient client, Dictionary<string, string> form) =>
        client.PostAsync("/connect/token", new FormUrlEncodedContent(form));

    public static async Task<TokenSet> ReadTokensAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        return new TokenSet(
            root.GetProperty("access_token").GetString()!,
            root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null,
            root.TryGetProperty("id_token", out var id) ? id.GetString() : null);
    }

    public static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
    }

    public static async Task<string?> ReadErrorDescriptionAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("error_description", out var value) ? value.GetString() : null;
    }
}
