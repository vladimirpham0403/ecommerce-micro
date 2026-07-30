using Ecommerce.BuildingBlocks.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Ecommerce.BuildingBlocks.Http;

/**
 * Dựng response lỗi VALIDATION_ERROR từ ModelState — dùng trong ApiBehaviorOptions.InvalidModelStateResponseFactory
 * để mọi lỗi bind/validate trả về đúng format ApiResponse thay vì ValidationProblemDetails mặc định.
 */
public static class ValidationResponse
{
    public static ApiResponse<object> From(ModelStateDictionary modelState, HttpContext context)
    {
        var details = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var error = new ApiError(ErrorCodes.ValidationError, "One or more validation errors occurred.", details);
        return ApiResponse.Fail(error, ApiMeta.From(context));
    }
}
