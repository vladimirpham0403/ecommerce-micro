using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ecommerce.Product.Data;

/**
 * Chỉ dùng ở design-time (dotnet ef migrations / database update).
 * Runtime đăng ký DbContext trong Program.cs.
 * Lưu ý: phải dùng cùng cấu hình UseSnakeCaseNamingConvention() ở Program.cs để model khớp với migration.
 */
public class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("ProductDb") ?? "Host=localhost;Port=5432;Database=product_db;Username=ecom;Password=ecom_dev_password";

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new ProductDbContext(options);
    }
}
