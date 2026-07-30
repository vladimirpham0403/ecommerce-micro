using Ecommerce.Product.Domain;
using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Mapping;

public static class MappingExtensions
{
    extension(Domain.Product p)
    {
        public ProductResponse ToResponse() => new(
            p.Id, p.Sku, p.Name, p.Slug, p.Description,
            p.BrandId, p.Brand?.Name ?? string.Empty,
            p.CategoryId, p.Category?.Name ?? string.Empty,
            p.Price, p.Currency, p.Status, p.Attributes,
            p.CreatedAt, p.UpdatedAt,
            p.Variants.Select(v => v.ToDto()).ToList(),
            p.Images.Select(i => i.ToDto()).ToList());

        public ProductListItem ToListItem() => new(
            p.Id, p.Sku, p.Name, p.Slug, p.Price, p.Currency, p.Status, p.Brand?.Name ?? string.Empty,
            p.Category?.Name ?? string.Empty, p.CreatedAt);
    }

    extension(ProductVariant v)
    {
        public ProductVariantDto ToDto() =>
            new(v.Id, v.Sku, v.Name, v.Price, v.Attributes, v.IsActive);
    }

    extension(ProductImage i)
    {
        public ProductImageDto ToDto() =>
            new(i.Id, i.VariantId, i.Url, i.AltText, i.SortOrder, i.IsPrimary);
    }

    extension(Category c)
    {
        public CategoryResponse ToResponse() =>
            new(c.Id, c.Name, c.Slug, c.Description, c.ParentId, c.IsActive, c.CreatedAt, c.UpdatedAt);
    }

    extension(Brand b)
    {
        public BrandResponse ToResponse() =>
            new(b.Id, b.Name, b.Slug, b.LogoUrl, b.Description, b.IsActive, b.CreatedAt, b.UpdatedAt);
    }
}