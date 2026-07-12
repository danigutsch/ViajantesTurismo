using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Owns the Management Web Data Protection key ring stored in the security database.
/// </summary>
internal sealed class ManagementSecurityDbContext(DbContextOptions<ManagementSecurityDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<DataProtectionKey>()
            .ToTable("data_protection_keys", ManagementAuthenticationDefaults.SecurityStoreSchemaName);
    }
}
