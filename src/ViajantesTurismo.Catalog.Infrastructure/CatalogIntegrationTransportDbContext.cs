using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// EF Core context for the shared PostgreSQL integration-event transport table.
/// </summary>
internal sealed class CatalogIntegrationTransportDbContext(
    DbContextOptions<CatalogIntegrationTransportDbContext> options,
    IEnumerable<IDbContextConfiguration<CatalogIntegrationTransportDbContext>>? configurations = null) : DbContext(options)
{
    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureConventions(configurationBuilder);
            }
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        if (configurations is not null)
        {
            foreach (var configuration in configurations)
            {
                configuration.ConfigureModel(modelBuilder);
            }
        }
    }
}
