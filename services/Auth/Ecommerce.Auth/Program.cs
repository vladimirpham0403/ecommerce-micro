using System.Net;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Ecommerce.Auth.Common;
using Ecommerce.Auth.Data;
using Ecommerce.Auth.Domain.Contracts;
using Ecommerce.Auth.Services;
using Ecommerce.Auth.Services.Impl;
using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.BuildingBlocks.Middleware;
using Ecommerce.BuildingBlocks.Persistence.Auditing;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;
using static OpenIddict.Abstractions.OpenIddictConstants;

const string devTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";
const string PasswordScheme = "oauth2-password";
const string AuthorizationCodeScheme = "oauth2-authorization-code";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: devTemplate, theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    if (ctx.HostingEnvironment.IsDevelopment())
        cfg.WriteTo.Console(outputTemplate: devTemplate);
    else
        cfg.WriteTo.Console(new JsonFormatter());
});

var connectionString = builder.Configuration.GetConnectionString("AuthDb")
                       ?? throw new InvalidOperationException("Connection string 'AuthDb' not found.");

builder.Services.AddScoped<AuditableSaveChangesInterceptor>();
builder.Services.AddDbContext<AuthDbContext>((sp, options) =>
{
    options.AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>());
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null));
    options.UseSnakeCaseNamingConvention();
    options.UseOpenIddict();

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

var forwardedHeadersSection = builder.Configuration.GetSection("Auth:ForwardedHeaders");
var forwardedHeadersEnabled = forwardedHeadersSection.GetValue("Enabled", false);

if (forwardedHeadersEnabled)
{
    var knownNetworks = forwardedHeadersSection.GetSection("KnownNetworks").Get<string[]>() ?? [];
    var knownProxies = forwardedHeadersSection.GetSection("KnownProxies").Get<string[]>() ?? [];

    if (knownNetworks.Length == 0 && knownProxies.Length == 0)
    {
        throw new InvalidOperationException(
            "Bật 'Auth:ForwardedHeaders:Enabled' thì phải khai KnownNetworks hoặc KnownProxies. " +
            "Tin X-Forwarded-For từ mọi nguồn là cho phép client tự khai IP để vượt rate-limit.");
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        options.ForwardLimit = forwardedHeadersSection.GetValue("ForwardLimit", 1);

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var network in knownNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }

        foreach (var proxy in knownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }
    });
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = false;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "ecom.auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.ManageUsers, policy => policy
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
            ctx.User.GetClaims(Claims.Role).Contains(RoleNames.Admin, StringComparer.Ordinal) &&
            ctx.User.HasScope(ScopeNames.UsersManage)));
});

var issuerValue = builder.Configuration["Auth:Issuer"]
                  ?? throw new InvalidOperationException("Cấu hình 'Auth:Issuer' không được để trống.");
var issuer = new Uri(issuerValue, UriKind.Absolute);

var signingCertificates = new SigningCertificateInventory();
builder.Services.AddSingleton(signingCertificates);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOpenIddict()
    .AddCore(options => options
        .UseEntityFrameworkCore()
        .UseDbContext<AuthDbContext>())

    .AddServer(options =>
    {
        options.SetIssuer(issuer);

        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout")
            .SetUserInfoEndpointUris("connect/userinfo")
            .SetIntrospectionEndpointUris("connect/introspect")
            .SetRevocationEndpointUris("connect/revoke");

        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange()
            .AllowPasswordFlow()
            .AllowRefreshTokenFlow()
            .AllowClientCredentialsFlow();

        options.RegisterScopes(
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess,
            ScopeNames.ProductRead,
            ScopeNames.ProductWrite,
            ScopeNames.UsersManage);

        options.RegisterAudiences(Audiences.ProductApi, Audiences.AuthApi);

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
            .SetIdentityTokenLifetime(TimeSpan.FromMinutes(15))
            .SetRefreshTokenLifetime(TimeSpan.FromDays(14));

        options.DisableAccessTokenEncryption();

        options.SetRefreshTokenReuseLeeway(
            TimeSpan.FromSeconds(builder.Configuration.GetValue("Auth:RefreshTokenReuseLeewaySeconds", 0)));

        options.AddEcommerceCredentials(builder.Configuration, builder.Environment, signingCertificates);

        var aspNetCore = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableStatusCodePagesIntegration();

        if (!issuer.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    })

    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

var dataProtectionPath = builder.Configuration["Auth:DataProtection:Path"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
        .SetApplicationName("ecommerce-auth");
}

builder.Services.AddRateLimiter(options =>
{
    var permitLimit = builder.Configuration.GetValue("Auth:RateLimit:LoginPermitLimit", 10);

    options.AddPolicy(RateLimitPolicies.Login, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var error = new ApiError(ErrorCodes.SystemTooManyRequests, "Too many requests. Slow down.");
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse.Fail(error, ApiMeta.From(ctx.HttpContext)), ct);
    };
});

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var swaggerScopes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Scopes.OpenId] = "Định danh người dùng",
        [Scopes.Email] = "Địa chỉ email",
        [Scopes.Profile] = "Thông tin hiển thị",
        [Scopes.Roles] = "Danh sách role",
        [Scopes.OfflineAccess] = "Cấp kèm refresh token",
        [ScopeNames.ProductRead] = "Đọc catalog",
        [ScopeNames.ProductWrite] = "Ghi catalog",
        [ScopeNames.UsersManage] = "Quản trị người dùng"
    };

    options.AddSecurityDefinition(PasswordScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Đường tắt cho client first-party. Không dùng cho app bên thứ ba.",
        Flows = new OpenApiOAuthFlows
        {
            Password = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                Scopes = swaggerScopes
            }
        }
    });

    options.AddSecurityDefinition(AuthorizationCodeScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Luồng chuẩn cho app người dùng cuối. Swagger tự sinh PKCE.",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("/connect/authorize", UriKind.Relative),
                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                Scopes = swaggerScopes
            }
        }
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(PasswordScheme, document)] = []
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(AuthorizationCodeScheme, document)] = []
    });
});

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
}).AddApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = ctx =>
        new BadRequestObjectResult(ValidationResponse.From(ctx.ModelState, ctx.HttpContext)));

builder.Services.AddScoped<IUserService, UserServiceImpl>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

builder.Services.AddScoped<ITokenPruner, TokenPruner>();
builder.Services.AddHostedService<TokenPruningService>();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"])
    .AddCheck<SigningCertificateHealthCheck>("signing-certificate", tags: ["ready"]);

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

if (forwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

app.UseCorrelationId();
app.UseEcommerceExceptionHandling();
app.UseSerilogRequestLogging(opts =>
{
    opts.GetLevel = (ctx, _, ex) =>
    {
        if (ex != null || ctx.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        var path = ctx.Request.Path.Value ?? string.Empty;
        var isNoise = path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                      || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                      || path.StartsWith("/ready", StringComparison.OrdinalIgnoreCase)
                      || path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);

        return isNoise ? LogEventLevel.Verbose : LogEventLevel.Information;
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.OAuthClientId(ClientIds.WebClient);
        o.OAuthUsePkce();
    });
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = h => h.Tags.Contains("ready") });

app.Run();
Log.CloseAndFlush();

public partial class Program;
