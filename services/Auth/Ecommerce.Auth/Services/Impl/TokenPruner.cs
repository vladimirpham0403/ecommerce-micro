using OpenIddict.Abstractions;

namespace Ecommerce.Auth.Services.Impl;

public sealed class TokenPruner(IOpenIddictTokenManager tokens, IOpenIddictAuthorizationManager authorizations, ILogger<TokenPruner> logger) : ITokenPruner
{
    public async Task PruneAsync(TimeSpan retention, CancellationToken ct)
    {
        var threshold = DateTimeOffset.UtcNow - retention;

        var prunedTokens = await tokens.PruneAsync(threshold, ct);
        var prunedAuthorizations = await authorizations.PruneAsync(threshold, ct);

        if (prunedTokens > 0 || prunedAuthorizations > 0)
        {
            logger.LogInformation(
                "Pruned {TokenCount} token(s) and {AuthorizationCount} authorization(s) created before {Threshold:o}",
                prunedTokens, prunedAuthorizations, threshold);
        }
        else
        {
            logger.LogDebug("Nothing to prune before {Threshold:o}", threshold);
        }
    }
}
