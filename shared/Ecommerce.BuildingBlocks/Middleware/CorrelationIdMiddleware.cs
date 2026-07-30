using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ecommerce.BuildingBlocks.Middleware;

/**
 * Nhận X-Correlation-Id từ client (nếu có) hoặc tự sinh; sinh thêm request-id riêng cho request này.
 * Gắn cả hai vào HttpContext.Items + log scope + response header, để mọi logvà response đều trace được.
 * Đăng ký trong pipeline (trước logging/exception).
 */
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "Ecommerce.CorrelationId";
    public const string RequestIdItemKey = "Ecommerce.RequestId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : Guid.NewGuid().ToString();

        var requestId = Guid.NewGuid().ToString();

        context.Items[CorrelationIdItemKey] = correlationId;
        context.Items[RequestIdItemKey] = requestId;

        // Trả lại correlation-id cho client; set trước khi response bắt đầu ghi.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var scope = new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["requestId"] = requestId,
        };

        using (logger.BeginScope(scope))
        {
            await next(context);
        }
    }
}
