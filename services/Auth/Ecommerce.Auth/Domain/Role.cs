using Ecommerce.BuildingBlocks.Persistence.Auditing;

namespace Ecommerce.Auth.Domain;

public class Role : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
