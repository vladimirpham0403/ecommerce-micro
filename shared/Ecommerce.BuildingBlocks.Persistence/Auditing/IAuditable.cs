namespace Ecommerce.BuildingBlocks.Persistence.Auditing;

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
