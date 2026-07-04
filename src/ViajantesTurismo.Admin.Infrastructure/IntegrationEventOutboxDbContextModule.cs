using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Infrastructure.ModelConfigurations;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class IntegrationEventOutboxDbContextModule : IAdminWriteDbContextModule
{
    public void Configure(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
    }

    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new IntegrationEventOutboxMessageConfiguration());
    }
}
