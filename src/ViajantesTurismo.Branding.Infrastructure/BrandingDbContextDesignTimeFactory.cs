using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Creates a <see cref="BrandingDbContext"/> for EF Core design-time tooling such as migrations.</summary>
internal sealed class BrandingDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BrandingDbContext>
{
    /// <summary>Initializes a new instance of the <see cref="BrandingDbContextDesignTimeFactory"/> class.</summary>
    public BrandingDbContextDesignTimeFactory()
    {
    }

    /// <inheritdoc />
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
