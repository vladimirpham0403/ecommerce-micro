using Ecommerce.Product.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Product.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Url).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.AltText).HasMaxLength(300);
        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Variant)
            .WithMany(v => v.Images)
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(i => !i.Product.IsDeleted);
    }
}
