namespace Ecommerce.Product.Dtos;

public record CreateCategoryRequest(string Name, string? Description, Guid? ParentId, bool IsActive = true);

public record UpdateCategoryRequest(string Name, string? Description, Guid? ParentId, bool IsActive);

public record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
