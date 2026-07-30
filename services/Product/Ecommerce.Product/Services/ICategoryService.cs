using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct);
    Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<CategoryResponse> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}