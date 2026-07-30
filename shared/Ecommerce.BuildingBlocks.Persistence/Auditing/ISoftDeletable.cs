namespace Ecommerce.BuildingBlocks.Persistence.Auditing;

/** Entity bị xóa mềm thay vì xóa cứng (interceptor chuyển Deleted -> Modified). */
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
