using Ecommerce.Product.Common;
using Ecommerce.Product.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Product.Data;

public static class ProductSeeder
{
    extension(IServiceProvider services)
    {
        /**
         * Apply migration + seed dữ liệu mẫu. Gọi lúc khởi động (Program.cs).
         * Migration luôn chạy; seed chỉ chạy ở Development và chỉ khi bảng products còn rỗng.
         */
        public async Task MigrateAndSeedAsync()
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ProductDbContext>();

            await db.Database.MigrateAsync();

            var env = sp.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsDevelopment())
            {
                return;
            }

            if (await db.Products.AnyAsync())
            {
                return;
            }

            const string brandName = "Anker";
            var brand = new Brand
            {
                Name = brandName,
                Slug = Slug.GenerateSlug(brandName),
                Description = "Phụ kiện điện tử",
                LogoUrl = "https://picsum.photos/seed/anker/200/200"
            };

            const string categoryName = "Tai nghe";
            var category = new Category
            {
                Name = categoryName,
                Slug = Slug.GenerateSlug(categoryName),
                Description = "Tai nghe có dây và không dây"
            };

            const string productName = "Tai nghe Bluetooth Anker Soundcore Q30";
            var product = new Domain.Product
            {
                Sku = "ANK-Q30",
                Name = productName,
                Slug = Slug.GenerateSlug(productName),
                Description = "Tai nghe chụp tai chống ồn chủ động, pin 40 giờ.",
                Brand = brand,
                Category = category,
                Price = 1_690_000m,
                Currency = "VND",
                Status = ProductStatus.Active,
                Attributes = """{"connectivity":"Bluetooth 5.0","battery_hours":40,"anc":true}"""
            };

            var black = new ProductVariant
            {
                Sku = "ANK-Q30-BLK", Name = $"{productName} - Đen", Attributes = """{"color":"black"}"""
            };

            var blue = new ProductVariant
            {
                Sku = "ANK-Q30-BLU",
                Name = $"{productName} - Xanh",
                Price = 1_750_000m, // biến thể có giá riêng, khác giá product
                Attributes = """{"color":"blue"}"""
            };

            product.Variants = [black, blue];

            product.Images =
            [
                new ProductImage
                {
                    Url = "https://picsum.photos/seed/q30-main/800/800",
                    AltText = productName,
                    SortOrder = 0,
                    IsPrimary = true
                },
                new ProductImage
                {
                    Url = "https://picsum.photos/seed/q30-black/800/800",
                    AltText = $"{productName} - Đen",
                    SortOrder = 1,
                    Variant = black // ảnh gắn riêng cho 1 biến thể
                }
            ];

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
    }
}
