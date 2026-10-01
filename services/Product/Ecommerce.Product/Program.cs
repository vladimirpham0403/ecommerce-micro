using Asp.Versioning;
using Ecommerce.BuildingBlocks.Auth;
using Ecommerce.BuildingBlocks.Http;
using Ecommerce.BuildingBlocks.Middleware;
using Ecommerce.BuildingBlocks.Persistence.Auditing;
using Ecommerce.Product.Common;
using Ecommerce.Product.Data;
using Ecommerce.Product.Services;
using Ecommerce.Product.Services.Impl;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.SystemConsole.Themes;

const string devTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}";

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

var connectionString = builder.Configuration.GetConnectionString("ProductDb")
                       ?? throw new InvalidOperationException("Connection string 'ProductDb' not found.");

builder.Services.AddScoped<AuditableSaveChangesInterceptor>();
builder.Services.AddDbContext<ProductDbContext>((sp, options) =>
{
    options.AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>());
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });

    options.UseSnakeCaseNamingConvention();
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Nút "Authorize" trong Swagger UI: dán access token lấy từ Auth service vào là gọi
    // được các endpoint ghi. Product không tự cấp token nên chỉ cần ô nhập bearer.
    const string scheme = "bearer";

    options.AddSecurityDefinition(scheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token lấy từ POST {Auth}/connect/token. Chỉ dán phần token, không kèm chữ 'Bearer'."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(scheme, document)] = []
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

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<ApiBehaviorOptions>(o =>
    o.InvalidModelStateResponseFactory = ctx =>
        new BadRequestObjectResult(ValidationResponse.From(ctx.ModelState, ctx.HttpContext)));

// Verify JWT bằng JWKS của Auth. Product không giữ secret nào - chỉ biết một URL.
builder.Services.AddEcommerceJwtBearer(builder.Configuration, ProductPolicies.Audience);

builder.Services.AddAuthorization(options =>
{
    // Vừa phải đúng role, vừa phải có scope: role nói "ai", scope nói "token này được phép làm gì".
    // Token của một admin nhưng chỉ xin scope product.read thì vẫn không ghi được.
    options.AddPolicy(ProductPolicies.Write, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(ProductPolicies.AdminRole)
        .RequireScope(ProductPolicies.WriteScope));
});

builder.Services.AddScoped<IProductService, ProductServiceImpl>();
builder.Services.AddScoped<ICategoryService, CategoryServiceImpl>();
builder.Services.AddScoped<IBrandService, BrandServiceImpl>();

builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

var app = builder.Build();

await app.Services.MigrateAndSeedAsync();

app.UseCorrelationId(); // sinh/nhận X-Correlation-Id (sớm nhất)
app.UseEcommerceExceptionHandling(); // bắt AppException -> ApiResponse lỗi (ngay sau correlation)
app.UseSerilogRequestLogging(opts =>  // log 1 dòng/request (đã có correlation id trong scope)
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(); // Swagger UI: /swagger
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }); // liveness
app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = h => h.Tags.Contains("ready") }); // readiness (check Postgres)

app.Run();
Log.CloseAndFlush();

public partial class Program;
