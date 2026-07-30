using Ecommerce.Product.Domain;

namespace Ecommerce.Product.Dtos;

public record ProductVariantDto(Guid Id, string Sku, string Name, decimal? Price, string? Attributes, bool IsActive);

public record ProductImageDto(Guid Id, Guid? VariantId, string Url, string? AltText, int SortOrder, bool IsPrimary);

public record CreateProductVariantRequest(string Sku, string Name, decimal? Price, string? Attributes, bool IsActive = true);

public record CreateProductImageRequest(string Url, string? AltText, int SortOrder, bool IsPrimary);

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    decimal Price,
    string Currency,
    ProductStatus Status,
    string? Attributes,
    List<CreateProductVariantRequest>? Variants,
    List<CreateProductImageRequest>? Images);

public record UpdateProductRequest(
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    decimal Price,
    string Currency,
    ProductStatus Status,
    string? Attributes);

public record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    Guid BrandId,
    string BrandName,
    Guid CategoryId,
    string CategoryName,
    decimal Price,
    string Currency,
    ProductStatus Status,
    string? Attributes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images);

public record ProductListItem(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    decimal Price,
    string Currency,
    ProductStatus Status,
    string BrandName,
    string CategoryName,
    DateTime CreatedAt);

/** Tham số list: paging + filter + sort, bind từ query string. */
public record ProductListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public string? Search { get; init; }
    public string Sort { get; init; } = "createdAt";   // price | name | createdAt
    public string Order { get; init; } = "desc";       // asc | desc
}
