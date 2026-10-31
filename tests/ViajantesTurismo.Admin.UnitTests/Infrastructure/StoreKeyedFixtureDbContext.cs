using Microsoft.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>A deliberately store-keyed model that the client-generated key convention must reject.</summary>
internal sealed class StoreKeyedFixtureDbContext(DbContextOptions<StoreKeyedFixtureDbContext> options) : DbContext(options)
{
    /// <summary>Rows whose integer key the database assigns.</summary>
    public DbSet<StoreKeyedRow> StoreKeyedRows => Set<StoreKeyedRow>();

    /// <summary>Rows whose key the caller assigns.</summary>
    public DbSet<ClientKeyedRow> ClientKeyedRows => Set<ClientKeyedRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<StoreKeyedRow>().HasKey(row => row.Id);
        modelBuilder.Entity<ClientKeyedRow>().Property(row => row.Id).ValueGeneratedNever();
    }
}
