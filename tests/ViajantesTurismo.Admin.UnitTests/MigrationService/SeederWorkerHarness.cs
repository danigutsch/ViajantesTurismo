using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class SeederWorkerHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly Func<IServiceProvider, CancellationToken, Task> seedOperation;

    private SeederWorkerHarness(
        ServiceProvider serviceProvider,
        TestHostApplicationLifetime hostLifetime,
        Func<IServiceProvider, CancellationToken, Task> seedOperation)
    {
        this.serviceProvider = serviceProvider;
        this.seedOperation = seedOperation;
        HostLifetime = hostLifetime;
    }

    public TestHostApplicationLifetime HostLifetime { get; }

    public static SeederWorkerHarness Create(Func<CancellationToken, Task> seedOperation)
    {
        ArgumentNullException.ThrowIfNull(seedOperation);

        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        return new SeederWorkerHarness(
            services.BuildServiceProvider(),
            new TestHostApplicationLifetime(),
            (_, ct) => seedOperation(ct));
    }

    public SeederWorker CreateWorker()
    {
        return new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            HostLifetime,
            seedOperation);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
