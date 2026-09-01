using Ecommerce.Auth.Common;
using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Domain.Contracts;
using Ecommerce.Auth.Services;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Ecommerce.Auth.Data;

public static class AuthSeeder
{
    extension(IServiceProvider services)
    {
        public async Task MigrateAndSeedAsync()
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<AuthDbContext>();

            await db.Database.MigrateAsync();

            await SeedRolesAsync(db);
            await SeedScopesAsync(sp.GetRequiredService<IOpenIddictScopeManager>());
            await SeedApplicationsAsync(
                sp.GetRequiredService<IOpenIddictApplicationManager>(),
                sp.GetRequiredService<IConfiguration>());

            if (sp.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                await SeedDevelopmentUsersAsync(db, sp.GetRequiredService<IPasswordHasher>());
            }
        }
    }

    private static async Task SeedRolesAsync(AuthDbContext db)
    {
        var existing = await db.Roles.Select(r => r.Name).ToListAsync();
        var missing = RoleNames.All.Except(existing, StringComparer.Ordinal).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.Roles.AddRange(missing.Select(name => new Role
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = name switch
            {
                RoleNames.Admin => "Toàn quyền quản trị catalog và người dùng",
                RoleNames.Manager => "Quản lý nghiệp vụ, chưa dùng ở Phase 1",
                _ => "Khách hàng - quyền mặc định khi đăng ký"
            }
        }));

        await db.SaveChangesAsync();
    }

    private static async Task SeedScopesAsync(IOpenIddictScopeManager manager)
    {
        var descriptors = new[]
        {
            (Name: ScopeNames.ProductRead, Display: "Đọc dữ liệu catalog", Audience: Audiences.ProductApi),
            (Name: ScopeNames.ProductWrite, Display: "Tạo, sửa, xóa dữ liệu catalog", Audience: Audiences.ProductApi),
            (Name: ScopeNames.UsersManage, Display: "Quản trị người dùng", Audience: Audiences.AuthApi)
        };

        foreach (var (name, display, audience) in descriptors)
        {
            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = display,
                Resources = { audience }
            };

            if (await manager.FindByNameAsync(name) is { } existing)
            {
                await manager.UpdateAsync(existing, descriptor);
                continue;
            }

            await manager.CreateAsync(descriptor);
        }
    }

    private static async Task SeedApplicationsAsync(
        IOpenIddictApplicationManager manager,
        IConfiguration configuration)
    {
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = ClientIds.WebClient,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "Ecommerce Web Client",
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Revocation,
                    Permissions.Endpoints.Introspection,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + ScopeNames.ProductRead,
                    Permissions.Prefixes.Scope + ScopeNames.ProductWrite,
                    Permissions.Prefixes.Scope + ScopeNames.UsersManage
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange }
            };

            foreach (var uri in ReadUris(configuration, "Auth:WebClient:RedirectUris"))
            {
                descriptor.RedirectUris.Add(uri);
            }

            foreach (var uri in ReadUris(configuration, "Auth:WebClient:PostLogoutRedirectUris"))
            {
                descriptor.PostLogoutRedirectUris.Add(uri);
            }

            descriptor.AddAudiencePermissions(Audiences.ProductApi, Audiences.AuthApi);

            await CreateOrUpdateAsync(manager, descriptor);
        }

        {
            var secret = configuration["Auth:ServiceWorker:Secret"]
                         ?? throw new InvalidOperationException(
                             "Cấu hình 'Auth:ServiceWorker:Secret' không được để trống.");

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = ClientIds.ServiceWorker,
                ClientSecret = secret,
                ClientType = ClientTypes.Confidential,
                DisplayName = "Internal service worker",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Introspection,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + ScopeNames.ProductRead
                }
            };

            descriptor.AddAudiencePermissions(Audiences.ProductApi);

            await CreateOrUpdateAsync(manager, descriptor);
        }
    }

    private static async Task CreateOrUpdateAsync(
        IOpenIddictApplicationManager manager,
        OpenIddictApplicationDescriptor descriptor)
    {
        var existing = await manager.FindByClientIdAsync(descriptor.ClientId!);

        if (existing is null)
        {
            await manager.CreateAsync(descriptor);
            return;
        }

        await manager.UpdateAsync(existing, descriptor);
    }

    private static IEnumerable<Uri> ReadUris(IConfiguration configuration, string key)
    {
        var values = configuration.GetSection(key).Get<string[]>();

        if (values is null || values.Length == 0)
        {
            throw new InvalidOperationException($"Cấu hình '{key}' không được để trống.");
        }

        return values.Select(value => new Uri(value, UriKind.Absolute));
    }

    private static async Task SeedDevelopmentUsersAsync(AuthDbContext db, IPasswordHasher hasher)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, StringComparer.Ordinal);

        db.Users.AddRange(
            NewUser("admin@ecom.local", "Quản trị viên", "Admin@123456", [RoleNames.Admin, RoleNames.Customer]),
            NewUser("customer@ecom.local", "Khách hàng mẫu", "Customer@123456", [RoleNames.Customer]));

        await db.SaveChangesAsync();

        AppUser NewUser(string email, string displayName, string password, string[] roleNames)
        {
            var user = new AppUser
            {
                Id = Guid.CreateVersion7(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = displayName,
                PasswordHash = hasher.Hash(password)
            };

            foreach (var roleName in roleNames)
            {
                user.UserRoles.Add(new UserRole { Role = roles[roleName] });
            }

            return user;
        }
    }
}
