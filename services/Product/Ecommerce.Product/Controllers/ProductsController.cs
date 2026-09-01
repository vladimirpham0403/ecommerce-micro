using Asp.Versioning;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Common;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Product.Controllers;

/**
 * ProductsController — gọi IProductService, bọc kết quả bằng ApiResponse.Ok(data, ApiMeta.From(HttpContext)).
 * Không try/catch — AppException do service ném đã được ExceptionHandlingMiddleware xử lý.
 */
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/products")]
public class ProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProductListQuery query, CancellationToken ct)
    {
        var result = await service.ListAsync(query, ct);
        return Ok(ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return Ok(ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpPost]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/v1/products/{result.Id}", ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return Ok(ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
