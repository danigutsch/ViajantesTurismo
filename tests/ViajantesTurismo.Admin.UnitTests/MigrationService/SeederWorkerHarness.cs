using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Branding;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
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

    public static SeederWorkerHarness CreateWithDefaultInitialization()
    {
        var services = new ServiceCollection();
        var adminDatabaseRoot = new InMemoryDatabaseRoot();
        var adminDatabaseName = Guid.NewGuid().ToString("N");
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddDbContext<BrandingDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddScoped<IBrandingSettingsStore, EmptyBrandingSettingsStore>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(adminDatabaseName, adminDatabaseRoot);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        services.AddScoped(sp => new Seeder(sp.GetRequiredService<AdminWriteDbContext>()));

        return new SeederWorkerHarness(
            services.BuildServiceProvider(),
            new TestHostApplicationLifetime(),
            (_, _) => Task.CompletedTask);
    }

    public SeederWorker CreateWorker()
    {
        return new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            HostLifetime,
            seedOperation);
    }

    public SeederWorker CreateDefaultWorker()
    {
        return new SeederWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SeederWorker>.Instance,
            HostLifetime);
    }

    public async Task ShouldContainSeedData(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var tourCount = await dbContext.Tours.CountAsync(ct);
        var customerCount = await dbContext.Customers.CountAsync(ct);
        var bookingCount = await dbContext.Set<Booking>().CountAsync(ct);

        tourCount.ShouldBe(5);
        customerCount.ShouldBe(15);
        bookingCount.ShouldBe(10);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
