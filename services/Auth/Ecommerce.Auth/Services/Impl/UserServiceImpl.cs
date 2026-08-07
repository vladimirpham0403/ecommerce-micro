using Ecommerce.Auth.Data;
using Ecommerce.Auth.Domain;
using Ecommerce.Auth.Domain.Contracts;
using Ecommerce.Auth.Dtos;
using Ecommerce.BuildingBlocks.Errors;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Auth.Services.Impl;

public class UserServiceImpl(AuthDbContext db, IPasswordHasher hasher, ILogger<UserServiceImpl> logger) : IUserService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

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
        var user = db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefault(u => u.NormalizedEmail == Normalize(email));

        if (user is null)
        {
            hasher.Hash(password);
            throw InvalidCredentials();
        }

        if (user.LockoutEndAt is { } until && until > DateTime.UtcNow)
        {
            throw AppException.BadRequest(ErrorCodes.AuthAccountLocked, "Account is locked.");
        }

        if (!user.IsActive)
        {
            throw InvalidCredentials();
        }

        if (!hasher.Verify(password, user.PasswordHash))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.LockoutEndAt = DateTime.UtcNow.Add(LockoutDuration);
                user.AccessFailedCount = 0;
                logger.LogWarning("User {Email} has been locked out.", user.Email);
            }

            await db.SaveChangesAsync(ct);
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

    private static string Normalize(string email)
    {
        return email.ToUpperInvariant();
    }

    private static AppException InvalidCredentials()
    {
        return AppException.Unauthorized(ErrorCodes.AuthInvalidCredentials, "Invalid email or password.");
    }
}
