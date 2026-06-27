using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class SeederWorkerHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;

    private SeederWorkerHarness(ServiceProvider serviceProvider, TestHostApplicationLifetime hostLifetime)
    {
        this.serviceProvider = serviceProvider;
        HostLifetime = hostLifetime;
    }

    public TestHostApplicationLifetime HostLifetime { get; }

    public static SeederWorkerHarness Create(ISeeder seeder)
    {
        ArgumentNullException.ThrowIfNull(seeder);

        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton(seeder);

        return new SeederWorkerHarness(
            services.BuildServiceProvider(),
            new TestHostApplicationLifetime());
    }

    public SeederWorker CreateWorker()
    {
        return new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            HostLifetime);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
