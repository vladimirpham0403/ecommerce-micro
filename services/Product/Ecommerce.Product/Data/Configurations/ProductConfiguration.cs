using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Product.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Domain.Product>
{
    public void Configure(EntityTypeBuilder<Domain.Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Slug).HasMaxLength(320).IsRequired();
        builder.Property(p => p.Price).HasPrecision(19, 4);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("VND");

        // Lưu enum dạng string cho dễ đọc trong DB.
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.Attributes).HasColumnType("jsonb");
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        // Partial unique: SKU/slug đã soft-delete không chiếm chỗ -> tạo lại cùng SKU được.
        builder.HasIndex(p => p.Sku).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.Slug).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.BrandId);

        // Soft delete: mọi query mặc định bỏ qua product đã xóa.
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
