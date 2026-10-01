using Asp.Versioning;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Common;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Product.Controllers;

/**
 * CategoriesController — gọi ICategoryService, bọc kết quả bằng ApiResponse.Ok(data, ApiMeta.From(HttpContext)).
 * Lỗi để service ném (ExceptionHandlingMiddleware xử lý).
 */
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/categories")]
public class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        return Ok(ApiResponse.Ok(await service.ListAsync(ct), ApiMeta.From(HttpContext)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        return Ok(ApiResponse.Ok(await service.GetByIdAsync(id, ct), ApiMeta.From(HttpContext)));
    }

    [HttpPost]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/v1/categories/{result.Id}", ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken ct)
    {
        return Ok(ApiResponse.Ok(await service.UpdateAsync(id, request, ct), ApiMeta.From(HttpContext)));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ProductPolicies.Write)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
