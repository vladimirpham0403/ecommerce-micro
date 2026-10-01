using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ecommerce.Auth.Common;

public sealed class SigningCertificateHealthCheck(SigningCertificateInventory inventory, IConfiguration configuration, TimeProvider clock) : IHealthCheck
{
    private readonly TimeSpan _warnBefore = TimeSpan.FromDays(configuration.GetValue("Auth:Certificates:ExpiryWarningDays", 30));

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (inventory.SigningValidUntil is not { } validUntil)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Không dùng chứng thư từ file."));
        }

        var remaining = validUntil - clock.GetUtcNow();
        var data = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["validUntil"] = validUntil.ToString("o"),
            ["daysRemaining"] = Math.Floor(remaining.TotalDays),
            ["certificateCount"] = inventory.Certificates.Count
        };

        if (remaining <= TimeSpan.Zero)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Chứng thư ký đã hết hạn lúc {validUntil:yyyy-MM-dd}. Auth không cấp được token.",
                data: data));
        }

        if (remaining <= _warnBefore)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Chứng thư ký hết hạn sau {remaining.Days} ngày ({validUntil:yyyy-MM-dd}). Cần xoay khóa.",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Chứng thư ký còn hạn tới {validUntil:yyyy-MM-dd}.", data));
    }
}
