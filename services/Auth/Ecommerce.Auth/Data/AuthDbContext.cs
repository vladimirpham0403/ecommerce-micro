using Ecommerce.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Auth.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options): DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
