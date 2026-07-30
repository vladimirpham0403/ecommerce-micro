using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ecommerce.BuildingBlocks.Middleware;

/**
 * Bắt mọi exception và chuyển thành ApiResponse lỗi theo format chuẩn.
 * AppException -> map đúng mã lỗi + status nó mang theo.
 * Exception khác -> 500 SYSTEM_INTERNAL_ERROR (không lộ chi tiết nội bộ ra client).
 * Đăng ký ngay sau CorrelationIdMiddleware để response lỗi cũng có meta đầy đủ.
 */
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Handled application error {ErrorCode}", ex.ErrorCode);
            await WriteAsync(context, ex.StatusCode, new ApiError(ex.ErrorCode, ex.Message, ex.Details));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                new ApiError(ErrorCodes.SystemInternalError, "An unexpected error occurred."));
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, ApiError error)
    {
        // Nếu response đã bắt đầu gửi thì không thể ghi đè -> bỏ qua, tránh ném tiếp.
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var body = ApiResponse.Fail(error, ApiMeta.From(context));
        await context.Response.WriteAsJsonAsync(body);
    }
}
