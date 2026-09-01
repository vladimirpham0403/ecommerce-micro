using System.Security.Claims;
using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.BuildingBlocks.Auth;

public static class EcommerceJwtBearerExtensions
{
    private const string ScopeClaimType = "scope";

    public static IServiceCollection AddEcommerceJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration,
        string audience)
    {
        var authority = configuration["Auth:Authority"]
                        ?? throw new InvalidOperationException("Cấu hình 'Auth:Authority' không được để trống.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = configuration.GetValue("Auth:RequireHttpsMetadata", true);

                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        var expired = IsTokenExpired(context.AuthenticateFailure);

                        return WriteErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            new ApiError(
                                expired ? ErrorCodes.AuthTokenExpired : ErrorCodes.AuthUnauthenticated,
                                expired ? "Access token has expired." : "Authentication is required."));
                    },
                    OnForbidden = context => WriteErrorAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        new ApiError(ErrorCodes.AuthForbidden, "Insufficient role or scope."))
                };
            });

        return services;
    }

    public static bool HasScope(this ClaimsPrincipal principal, string scope) =>
        principal.FindAll(ScopeClaimType)
            .Any(claim => claim.Value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(scope, StringComparer.Ordinal));

    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) =>
        builder.RequireAssertion(context => context.User.HasScope(scope));

    private static bool IsTokenExpired(Exception? exception) => exception switch
    {
        null => false,
        SecurityTokenExpiredException => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(IsTokenExpired),
        _ => IsTokenExpired(exception.InnerException)
    };

    private static Task WriteErrorAsync(HttpContext context, int statusCode, ApiError error)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(ApiResponse.Fail(error, ApiMeta.From(context)));
    }
}
