using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ecommerce.BuildingBlocks.Persistence.Auditing;

/**
 * Interceptor dùng chung cho mọi service: tự stamp CreatedAt/UpdatedAt (IAuditable) và chuyển xóa cứng thành soft delete (ISoftDeletable).
 *
 * Đăng ký trong Program.cs:
 *   builder.Services.AddScoped<AuditableSaveChangesInterceptor>();
 *   builder.Services.AddDbContext<ProductDbContext>((sp, o) => o
 *       .UseNpgsql(cs).UseSnakeCaseNamingConvention()
 *       .AddInterceptors(sp.GetRequiredService<AuditableSaveChangesInterceptor>()));
 */
public class AuditableSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Apply(DbContext? context)
    {
        if (context is null) return;
        
        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: IAuditable addded })
            {
                addded.CreatedAt = now;
                addded.UpdatedAt = now;
            }
            else if (entry is { State: EntityState.Modified, Entity: IAuditable modified })
            {
                modified.UpdatedAt = now;
            }

            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deletable })
            {
                entry.State = EntityState.Modified;
                deletable.IsDeleted = true;
                deletable.DeletedAt = now;
            }
        }
        
        // TODO: Sau nay se lay them User tu DI de hoan thien created by va updated by
    }
}
