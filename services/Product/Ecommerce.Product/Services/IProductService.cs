using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Dtos;

namespace Ecommerce.Product.Services;

public interface IProductService
{
    Task<PagedResult<ProductListItem>> ListAsync(ProductListQuery query, CancellationToken ct);
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}