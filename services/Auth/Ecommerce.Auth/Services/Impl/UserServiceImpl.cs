using Ecommerce.Auth.Data;
using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Domain.Contracts;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Errors;
using Ecommerce.BuildingBlocks.Http;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Auth.Services.Impl;

public class UserServiceImpl(AuthDbContext db, IPasswordHasher hasher, ILogger<UserServiceImpl> logger) : IUserService
{
    private const int MaxFailedAttempts = 5;
    private const int MaxPageSize = 100;
    private static readonly TimeSpan _lockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        string normalizedEmail = Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            throw AppException.Conflict(ErrorCodes.AuthEmailAlreadyUsed, "Email already registered.");
        }

        var customerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Customer, ct);
        if (customerRole is null)
        {
            logger.LogError("Customer role not found");
            throw new AppException(ErrorCodes.SystemInternalError, StatusCodes.Status500InternalServerError ,"Customer role not found.");
        }
        var user = new AppUser()
        {
            Id = Guid.CreateVersion7(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName?.Trim(),
            PasswordHash = hasher.Hash(request.Password)
        };
        user.UserRoles.Add(new UserRole { Role = customerRole });
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Email, user.DisplayName, [RoleNames.Customer]);
    }

    public async Task<AppUser> ValidateCredentialsAsync(string email, string password, CancellationToken ct)
    {
        var normalizedEmail = Normalize(email);
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            hasher.Hash(password);
            throw InvalidCredentials();
        }

        var passwordValid = hasher.Verify(password, user.PasswordHash);

        if (user.LockoutEndAt is { } until && until > DateTime.UtcNow)
        {
            throw AppException.BadRequest(ErrorCodes.AuthAccountLocked, "Account is locked.");
        }

        if (!passwordValid)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.LockoutEndAt = DateTime.UtcNow.Add(_lockoutDuration);
                user.AccessFailedCount = 0;
                logger.LogWarning("User {Email} has been locked out.", user.Email);
            }

            await db.SaveChangesAsync(ct);
            throw InvalidCredentials();
        }

        if (!user.IsActive)
        {
            throw InvalidCredentials();
        }

        user.AccessFailedCount = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return user;
    }

    public Task<AppUser?> FindActiveByIdAsync(Guid id, CancellationToken ct)
    {
        return db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id && u.IsActive, ct);
    }

    public async Task<PagedResult<UserSummaryResponse>> ListAsync(UserListQuery query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var size = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var users = db.Users.AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = Normalize(query.Search);
            users = users.Where(u =>
                u.NormalizedEmail.Contains(term) ||
                (u.DisplayName != null && u.DisplayName.ToUpper().Contains(term)));
        }

        var total = await users.LongCountAsync(ct);

        var items = await users
            .OrderBy(u => u.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<UserSummaryResponse>([.. items.Select(ToSummary)], page, size, total);
    }

    public async Task<UserSummaryResponse> SetRolesAsync(
        Guid userId, IReadOnlyList<string> roles, Guid actingUserId, CancellationToken ct)
    {
        var requested = roles.Distinct(StringComparer.Ordinal).ToList();

        var unknown = requested.Except(RoleNames.All, StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
        {
            throw AppException.BadRequest(
                ErrorCodes.AuthRoleNotFound,
                $"Unknown role(s): {string.Join(", ", unknown)}.",
                new { known = RoleNames.All });
        }

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            throw AppException.NotFound(ErrorCodes.AuthUserNotFound, "User not found.");
        }

        if (userId == actingUserId &&
            user.UserRoles.Any(ur => ur.Role.Name == RoleNames.Admin) &&
            !requested.Contains(RoleNames.Admin, StringComparer.Ordinal))
        {
            throw AppException.BadRequest(
                ErrorCodes.AuthCannotDemoteSelf,
                "You cannot remove the Admin role from your own account.");
        }

        var roleEntities = await db.Roles.Where(r => requested.Contains(r.Name)).ToListAsync(ct);

        user.UserRoles.Clear();
        foreach (var role in roleEntities)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Roles of user {UserId} set to [{Roles}] by {ActingUserId}", user.Id, string.Join(", ", requested), actingUserId);

        return ToSummary(user);
    }

    private static UserSummaryResponse ToSummary(AppUser user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.IsActive,
        [.. user.UserRoles.Select(ur => ur.Role.Name).Order(StringComparer.Ordinal)],
        user.CreatedAt,
        user.LastLoginAt);

    private static string Normalize(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static AppException InvalidCredentials()
    {
        return AppException.Unauthorized(ErrorCodes.AuthInvalidCredentials, "Invalid email or password.");
    }
}
