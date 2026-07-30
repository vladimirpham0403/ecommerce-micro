using Ecommerce.BuildingBlocks.Middleware;
using Microsoft.AspNetCore.Http;

namespace Ecommerce.BuildingBlocks.Http;

/// <summary>
/// Extensions đọc correlation-id / request-id mà <see cref="CorrelationIdMiddleware"/> đã gắn vào request.
/// </summary>
public static class HttpContextExtensions
{
    public static string GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.CorrelationIdItemKey, out var v) && v is string s
            ? s
            : string.Empty;

    public static string GetRequestId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.RequestIdItemKey, out var v) && v is string s
            ? s
            : string.Empty;
}
