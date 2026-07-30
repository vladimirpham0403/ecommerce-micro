using Ecommerce.BuildingBlocks.Persistence.Auditing;

namespace Ecommerce.Product.Domain;

public class Product : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }

    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    // Thuộc tính tự do (material, weight...) lưu dạng jsonb.
    public string? Attributes { get; set; }

    // Soft delete.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}
