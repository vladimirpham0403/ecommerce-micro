using System.Net;
using System.Net.Http.Json;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Http;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class RegisterAndLoginTests(AuthApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_user_with_customer_role()
    {
        var email = NewEmail();

        var resp = await _client.PostAsJsonAsync("/v1/auth/register",
            new RegisterRequest(email, "Passw0rd!", "Người dùng mới"));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.Equal(email, body.Data!.Email);
        Assert.Equal(["Customer"], body.Data.Roles);
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        var email = NewEmail();
        var request = new RegisterRequest(email, "Passw0rd!", null);

        var first = await _client.PostAsJsonAsync("/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("AUTH_EMAIL_ALREADY_USED", await ReadErrorCodeAsync(second));
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400()
    {
        var resp = await _client.PostAsJsonAsync("/v1/auth/register",
            new RegisterRequest(NewEmail(), "short", null));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await ReadErrorCodeAsync(resp));
    }

    [Fact]
    public async Task Email_is_matched_case_insensitively()
    {
        var email = NewEmail();
        await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, "Passw0rd!", null));

        var resp = await _client.PostAsJsonAsync("/v1/auth/register",
            new RegisterRequest(email.ToUpperInvariant(), "Passw0rd!", null));

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_five_times_locks_the_account()
    {
        var email = NewEmail();
        const string password = "Passw0rd!";
        await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, password, null));

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await _client.PasswordGrantAsync(email, "Wrong0rd!");
            Assert.Equal("Invalid email or password.", await OidcFlowHelper.ReadErrorDescriptionAsync(failed));
        }

        var locked = await _client.PasswordGrantAsync(email, password);

        Assert.False(locked.IsSuccessStatusCode);
        Assert.Contains(
            "locked",
            await OidcFlowHelper.ReadErrorDescriptionAsync(locked) ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Registered_user_can_immediately_get_a_token()
    {
        var email = NewEmail();
        const string password = "Passw0rd!";
        await _client.PostAsJsonAsync("/v1/auth/register", new RegisterRequest(email, password, "Tân binh"));

        var tokens = await _client.SignInAsync(email, password);

        Assert.False(string.IsNullOrEmpty(tokens.AccessToken));
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var resp = await _client.GetAsync("/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    private static string NewEmail() => $"user-{Guid.CreateVersion7():N}@ecom.test";

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return body?.Error?.Code;
    }
}
