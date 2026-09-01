namespace Ecommerce.Auth.Services.Impl;

public sealed class TokenPruningService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TokenPruningService> logger) : BackgroundService
{
    private readonly bool _enabled = configuration.GetValue("Auth:Pruning:Enabled", true);
    private readonly TimeSpan _interval = TimeSpan.FromHours(configuration.GetValue("Auth:Pruning:IntervalHours", 12));
    private readonly TimeSpan _retention = TimeSpan.FromDays(configuration.GetValue("Auth:Pruning:RetentionDays", 14));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            logger.LogWarning("Token pruning is disabled - openiddict_tokens will grow unbounded.");
            return;
        }

        using var timer = new PeriodicTimer(_interval);

        try
        {
            do
            {
                await PruneOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PruneOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ITokenPruner>().PruneAsync(_retention, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token pruning run failed; will retry in {Interval}", _interval);
        }
    }
}
