using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Management.Security;

/// <summary>
/// Owns persisted Data Protection keys for Management security state.
/// </summary>
public sealed class ManagementSecurityDbContext(DbContextOptions<ManagementSecurityDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    /// <inheritdoc />
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<DataProtectionKey>()
            .ToTable("data_protection_keys", ManagementSecurityDefaults.SchemaName);
    }
}
