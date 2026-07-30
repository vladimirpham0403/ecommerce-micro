using Asp.Versioning;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.Product.Dtos;
using Ecommerce.Product.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Product.Controllers;

/**
 * BrandsController — gọi IBrandService, bọc kết quả bằng ApiResponse.Ok(data, ApiMeta.From(HttpContext)).
 * Lỗi để service ném (ExceptionHandlingMiddleware xử lý).
 */
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/brands")]
public class BrandsController(IBrandService service) : ControllerBase
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
    public async Task<IActionResult> Create(CreateBrandRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/v1/brands/{result.Id}", ApiResponse.Ok(result, ApiMeta.From(HttpContext)));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBrandRequest request, CancellationToken ct)
    {
        return Ok(ApiResponse.Ok(await service.UpdateAsync(id, request, ct), ApiMeta.From(HttpContext)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
