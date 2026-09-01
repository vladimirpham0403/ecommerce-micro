namespace Ecommerce.Auth.Services;

public interface ITokenPruner
{
    public Task PruneAsync(TimeSpan retention, CancellationToken ct);
}
