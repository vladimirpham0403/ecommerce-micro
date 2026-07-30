using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Services;

public interface IBrandService
{
    Task<IReadOnlyList<BrandResponse>> ListAsync(CancellationToken ct);
    Task<BrandResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<BrandResponse> CreateAsync(CreateBrandRequest request, CancellationToken ct);
    Task<BrandResponse> UpdateAsync(Guid id, UpdateBrandRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}