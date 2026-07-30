namespace Ecommerce.Product.Domain;

public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Ảnh có thể gắn riêng cho một biến thể.
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }

    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; }
}
