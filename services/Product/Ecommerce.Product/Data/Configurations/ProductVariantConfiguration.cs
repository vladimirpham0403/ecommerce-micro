using Ecommerce.Product.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Product.Data.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(300).IsRequired();
        builder.Property(v => v.Price).HasPrecision(19, 4);
        builder.Property(v => v.Attributes).HasColumnType("jsonb");
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(v => v.UpdatedAt).HasDefaultValueSql("now()");
        
        builder.HasIndex(v => v.Sku).IsUnique();
        builder.HasIndex(v => v.ProductId);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Khớp query filter của Product để không lộ variant của product đã xóa.z
        builder.HasQueryFilter(v => !v.Product.IsDeleted);
    }
}
