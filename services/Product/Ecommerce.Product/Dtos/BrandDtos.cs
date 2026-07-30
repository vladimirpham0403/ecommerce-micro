namespace Ecommerce.Product.Dtos;

public record CreateBrandRequest(string Name, string? LogoUrl, string? Description, bool IsActive = true);

public record UpdateBrandRequest(string Name, string? LogoUrl, string? Description, bool IsActive);

public record BrandResponse(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
