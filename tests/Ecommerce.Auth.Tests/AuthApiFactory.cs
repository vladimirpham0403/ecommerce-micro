using Ecommerce.Auth.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Ecommerce.Auth.Tests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public const string RedirectUri = "https://localhost:5173/callback";

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__AuthDb", _db.GetConnectionString());
        Environment.SetEnvironmentVariable("Auth__Issuer", "http://localhost");
        Environment.SetEnvironmentVariable("Auth__Certificates__Ephemeral", "true");
        Environment.SetEnvironmentVariable("Auth__RateLimit__LoginPermitLimit", "10000");

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await ctx.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__AuthDb", null);
        Environment.SetEnvironmentVariable("Auth__Issuer", null);
        Environment.SetEnvironmentVariable("Auth__Certificates__Ephemeral", null);
        Environment.SetEnvironmentVariable("Auth__RateLimit__LoginPermitLimit", null);

        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class AuthApiCollection : ICollectionFixture<AuthApiFactory>
{
    public const string Name = "auth-api";
}
