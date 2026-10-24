using Microsoft.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;

namespace ViajantesTurismo.Admin.Infrastructure.Branding;

/// <summary>
/// Maps the Catalog-database <c>messaging.transport_messages</c> table so the Admin worker can claim rows for
/// consumer <c>admin</c>. Catalog Infrastructure owns the table's migrations; this context has none.
/// </summary>
internal sealed class AdminBrandingTransportDbContext(
    DbContextOptions<AdminBrandingTransportDbContext> options,
    IEnumerable<IDbContextConfiguration<AdminBrandingTransportDbContext>>? configurations = null) : DbContext(options)
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
