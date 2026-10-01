using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Ecommerce.Auth.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ecommerce.Auth.Tests;

public class SigningCertificateHealthCheckTests
{
    [Fact]
    public async Task Reports_healthy_when_no_certificate_is_loaded_from_file()
    {
        var result = await CheckAsync(new SigningCertificateInventory());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Reports_healthy_while_the_certificate_is_far_from_expiring()
    {
        var result = await CheckAsync(InventoryOf(TimeSpan.FromDays(200)));

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Reports_degraded_inside_the_warning_window()
    {
        var result = await CheckAsync(InventoryOf(TimeSpan.FromDays(10)));

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("xoay khóa", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Reports_unhealthy_once_the_certificate_has_expired()
    {
        var result = await CheckAsync(InventoryOf(TimeSpan.FromDays(-1)));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task An_old_certificate_alongside_a_fresh_one_does_not_raise_an_alarm()
    {
        var result = await CheckAsync(InventoryOf(TimeSpan.FromDays(3), TimeSpan.FromDays(400)));

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private static async Task<HealthCheckResult> CheckAsync(SigningCertificateInventory inventory)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Certificates:ExpiryWarningDays"] = "30"
            })
            .Build();

        var check = new SigningCertificateHealthCheck(inventory, configuration, TimeProvider.System);

        return await check.CheckHealthAsync(new HealthCheckContext());
    }

    private static SigningCertificateInventory InventoryOf(params TimeSpan[] remainingLifetimes)
    {
        var inventory = new SigningCertificateInventory();

        foreach (var remaining in remainingLifetimes)
        {
            inventory.Add(SelfSigned(remaining));
        }

        return inventory;
    }

    private static X509Certificate2 SelfSigned(TimeSpan remaining)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=ecommerce-auth-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var notAfter = DateTimeOffset.UtcNow + remaining;
        return request.CreateSelfSigned(notAfter - TimeSpan.FromDays(730), notAfter);
    }
}
