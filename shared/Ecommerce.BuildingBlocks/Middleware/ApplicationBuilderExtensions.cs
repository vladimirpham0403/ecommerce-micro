using Microsoft.AspNetCore.Builder;

namespace Ecommerce.BuildingBlocks.Middleware;

/**
 * Đăng ký các middleware dùng chung trong Programs.cs:
 * app.UseCorrelationId();
 * app.UseEcommerceExceptionHandling();
 */
public static class ApplicationBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public IApplicationBuilder UseCorrelationId() => app.UseMiddleware<CorrelationIdMiddleware>();
        public IApplicationBuilder UseEcommerceExceptionHandling() => app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
