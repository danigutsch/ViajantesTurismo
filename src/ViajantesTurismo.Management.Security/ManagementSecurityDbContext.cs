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

    internal DbSet<ManagementCookieTicketCacheEntry> TicketCacheEntries => Set<ManagementCookieTicketCacheEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<DataProtectionKey>()
            .ToTable("data_protection_keys", ManagementSecurityDefaults.SchemaName);

        modelBuilder.Entity<ManagementCookieTicketCacheEntry>(entity =>
        {
            entity.ToTable("management_cookie_tickets", ManagementSecurityDefaults.SchemaName);
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id)
                .HasMaxLength(449)
                .UseCollation("C")
                .HasColumnName("id");
            entity.Property(entry => entry.Value).HasColumnName("value");
            entity.Property(entry => entry.ExpiresAtTime).HasColumnName("expiresattime");
            entity.Property(entry => entry.SlidingExpirationInSeconds).HasColumnName("slidingexpirationinseconds");
            entity.Property(entry => entry.AbsoluteExpiration).HasColumnName("absoluteexpiration");
            entity.HasIndex(entry => entry.ExpiresAtTime).HasDatabaseName("ix_expiresattime");
        });
    }
}
