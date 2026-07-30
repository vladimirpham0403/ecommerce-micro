using Ecommerce.BuildingBlocks.Persistence.Auditing;

namespace Ecommerce.Product.Domain;

public class ProductVariant : IAuditable
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;

    // Giá riêng của biến thể; null = dùng giá của product.
    public decimal? Price { get; set; }

    // vd { "size": "M", "color": "red" }
    public string? Attributes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ProductImage> Images { get; set; } = [];
}
