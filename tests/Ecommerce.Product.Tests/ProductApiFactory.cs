using Ecommerce.Product.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Ecommerce.Product.Tests;

public sealed class ProductApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18-alpine").Build();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Development");

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        
        Environment.SetEnvironmentVariable("ConnectionStrings__ProductDb", _db.GetConnectionString());

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await ctx.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__ProductDb", null);
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ProductApiCollection : ICollectionFixture<ProductApiFactory>
{
    public const string Name = "product-api";
}
