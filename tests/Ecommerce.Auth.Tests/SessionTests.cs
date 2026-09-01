using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Http;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class SessionTests(AuthApiFactory factory)
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Signing_in_creates_exactly_one_listable_session()
    {
        var (client, _) = await NewUserSignedInAsync();

        var sessions = await ListSessionsAsync(client);

        var session = Assert.Single(sessions);
        Assert.Equal("web-client", session.ClientId);
        Assert.Contains("product.read", session.Scopes);
        Assert.True(session.IsCurrent, "Phiên của chính token đang gửi phải được đánh dấu IsCurrent.");

        Assert.NotNull(session.CreatedAt);
    }

    [Fact]
    public async Task Refreshing_reuses_the_same_session_instead_of_creating_a_new_one()
    {
        var (client, tokens) = await NewUserSignedInAsync();
        var before = Assert.Single(await ListSessionsAsync(client));

        var refreshed = await client.RefreshAsync(tokens.RefreshToken!);
        refreshed.EnsureSuccessStatusCode();

        var after = Assert.Single(await ListSessionsAsync(client));
        Assert.Equal(before.Id, after.Id);
    }

    [Fact]
    public async Task Revoking_all_sessions_kills_the_refresh_token()
    {
        var (client, tokens) = await NewUserSignedInAsync();

        var revoked = await client.DeleteAsync("/v1/auth/sessions");
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);

        var refreshed = await client.RefreshAsync(tokens.RefreshToken!);
        Assert.False(refreshed.IsSuccessStatusCode);
        Assert.Equal("invalid_grant", await OidcFlowHelper.ReadErrorAsync(refreshed));

        Assert.Empty(await ListSessionsAsync(client));
    }

    [Fact]
    public async Task Revoking_a_single_session_kills_only_that_one()
    {
        var (client, tokens) = await NewUserSignedInAsync();
        var session = Assert.Single(await ListSessionsAsync(client));

        var revoked = await client.DeleteAsync($"/v1/auth/sessions/{session.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);

        var refreshed = await client.RefreshAsync(tokens.RefreshToken!);
        Assert.False(refreshed.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Session_of_another_user_cannot_be_revoked()
    {
        var (victim, victimTokens) = await NewUserSignedInAsync();
        var (attacker, _) = await NewUserSignedInAsync();

        var victimSession = Assert.Single(await ListSessionsAsync(victim));

        var response = await attacker.DeleteAsync($"/v1/auth/sessions/{victimSession.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("AUTH_SESSION_NOT_FOUND", await ReadErrorCodeAsync(response));

        var stillWorks = await victim.RefreshAsync(victimTokens.RefreshToken!);
        Assert.True(stillWorks.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Listing_sessions_without_a_token_is_rejected()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await factory.CreateClient().GetAsync("/v1/auth/sessions")).StatusCode);
    }

    private async Task<(HttpClient Client, TokenSet Tokens)> NewUserSignedInAsync()
    {
        var client = factory.CreateClient();
        var email = $"session-{Guid.CreateVersion7():N}@ecom.test";

        var registered = await client.PostAsJsonAsync(
            "/v1/auth/register", new RegisterRequest(email, Password, null));
        registered.EnsureSuccessStatusCode();

        var tokens = await client.SignInAsync(email, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return (client, tokens);
    }

    private static async Task<IReadOnlyList<SessionResponse>> ListSessionsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/v1/auth/sessions");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SessionResponse>>>();
        return body!.Data!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        return body?.Error?.Code;
    }
}
