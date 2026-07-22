using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Branding;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.MigrationService;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class MigrationRunnerHarness : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly Func<IServiceProvider, CancellationToken, Task> seedOperation;

    private MigrationRunnerHarness(
        ServiceProvider serviceProvider,
        Func<IServiceProvider, CancellationToken, Task> seedOperation,
        MigrationStoreResolutionProbe? storeProbe = null)
    {
        this.serviceProvider = serviceProvider;
        this.seedOperation = seedOperation;
        StoreProbe = storeProbe;
        ActivitySource = new ActivitySource(MigrationRunner.ActivitySourceName);
    }

    public ActivitySource ActivitySource { get; }

    public MigrationStoreResolutionProbe? StoreProbe { get; }

    public static MigrationRunnerHarness Create(Func<CancellationToken, Task> seedOperation)
    {
        ArgumentNullException.ThrowIfNull(seedOperation);

        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        return new MigrationRunnerHarness(
            services.BuildServiceProvider(),
            (_, ct) => seedOperation(ct));
    }

    public static MigrationRunnerHarness CreateWithDefaultInitialization()
    {
        var services = new ServiceCollection();
        var adminDatabaseRoot = new InMemoryDatabaseRoot();
        var adminDatabaseName = Guid.NewGuid().ToString("N");
        var probe = new MigrationStoreResolutionProbe();
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var brandingOptions = new DbContextOptionsBuilder<BrandingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var securityOptions = new DbContextOptionsBuilder<ManagementSecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        services.AddScoped(_ =>
        {
            probe.RecordCatalog();
            return new CatalogDbContext(catalogOptions);
        });
        services.AddScoped(_ =>
        {
            probe.RecordBranding();
            return new BrandingDbContext(brandingOptions);
        });
        services.AddScoped(_ =>
        {
            probe.RecordManagementSecurity();
            return new ManagementSecurityDbContext(securityOptions);
        });
        services.AddScoped<IBrandingSettingsStore, EmptyBrandingSettingsStore>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(adminDatabaseName, adminDatabaseRoot);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        services.AddScoped(sp => new Seeder(sp.GetRequiredService<AdminWriteDbContext>()));

        return new MigrationRunnerHarness(
            services.BuildServiceProvider(),
            (_, _) => Task.CompletedTask,
            probe);
    }

    public MigrationRunner CreateRunner()
    {
        return new MigrationRunner(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger.Instance,
            seedOperation,
            ActivitySource);
    }

    public MigrationRunner CreateDefaultRunner()
    {
        return new MigrationRunner(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger.Instance,
            ActivitySource);
    }

    public MigrationRunner CreateDefaultRunner(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new MigrationRunner(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            loggerFactory);
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
        ActivitySource.Dispose();
        serviceProvider.Dispose();
    }
}
