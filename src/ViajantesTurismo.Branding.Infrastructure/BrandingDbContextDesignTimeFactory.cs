using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class BrandingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BrandingDbContext>
{
    public BrandingDbContextDesignTimeFactory()
    {
    }

    public BrandingDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.ConfigureIntegrationEventStorage<BrandingDbContext>(
            BrandingInfrastructureDependencyInjection.ConfigureBrandingIntegrationEventStorage);
        services.AddIntegrationEventOutbox<BrandingDbContext>();
        services.AddIntegrationEventTransportStorage<BrandingDbContext>();
        using var serviceProvider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=branding-design-time",
                providerOptions => providerOptions.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Branding",
                    schema: BrandingDbContext.MigrationsHistorySchemaName))
            .Options;

        return new BrandingDbContext(
            options,
            serviceProvider.GetServices<IDbContextConfiguration<BrandingDbContext>>());
    }
}
