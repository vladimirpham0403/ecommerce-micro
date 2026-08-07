using Ecommerce.BuildingBlocks.Persistence.Auditing;

namespace Ecommerce.Auth.Domain;

public class AppUser : IAuditable
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? DisplayName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
