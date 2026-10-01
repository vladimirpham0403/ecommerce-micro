using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Auth.Dtos;
using Ecommerce.Auth.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace Ecommerce.Auth.Tests;

[Collection(AuthApiCollection.Name)]
public class TokenPruningTests(AuthApiFactory factory)
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task Pruning_removes_dead_tokens_and_keeps_the_live_one()
    {
        var (client, tokens) = await NewUserSignedInAsync();

        var refreshed = await client.RefreshAsync(tokens.RefreshToken!);
        refreshed.EnsureSuccessStatusCode();
        var rotated = await OidcFlowHelper.ReadTokensAsync(refreshed);

        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();

        var before = await manager.CountAsync();
        await PruneAsync(scope.ServiceProvider);
        var after = await manager.CountAsync();

        Assert.True(after < before, $"Không dọn được token nào: trước {before}, sau {after}.");

        var stillWorks = await client.RefreshAsync(rotated.RefreshToken!);
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    [Fact]
    public async Task Pruning_removes_revoked_sessions_and_keeps_active_ones()
    {
        var (revoked, _) = await NewUserSignedInAsync();
        var (kept, keptTokens) = await NewUserSignedInAsync();

        (await revoked.DeleteAsync("/v1/auth/sessions")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();

        var before = await manager.CountAsync();
        await PruneAsync(scope.ServiceProvider);
        var after = await manager.CountAsync();

        Assert.True(after < before, $"Phiên đã thu hồi không được dọn: trước {before}, sau {after}.");

        var stillWorks = await kept.RefreshAsync(keptTokens.RefreshToken!);
        Assert.Equal(HttpStatusCode.OK, stillWorks.StatusCode);
    }

    private static Task PruneAsync(IServiceProvider services) =>
        services.GetRequiredService<ITokenPruner>().PruneAsync(TimeSpan.Zero, CancellationToken.None);

    private async Task<(HttpClient Client, TokenSet Tokens)> NewUserSignedInAsync()
    {
        var client = factory.CreateClient();
        var email = $"prune-{Guid.CreateVersion7():N}@ecom.test";

        var registered = await client.PostAsJsonAsync(
            "/v1/auth/register", new RegisterRequest(email, Password, null));
        registered.EnsureSuccessStatusCode();

        var tokens = await client.SignInAsync(email, Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return (client, tokens);
    }
}
