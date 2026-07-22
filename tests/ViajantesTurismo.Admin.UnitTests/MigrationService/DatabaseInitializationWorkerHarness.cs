using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Branding;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

internal sealed class DatabaseInitializationWorkerHarness : IDisposable
{
    private readonly Func<IServiceProvider, CancellationToken, Task> developmentDataOperation;
    private readonly IHostEnvironment environment;
    private readonly Func<IServiceProvider, CancellationToken, Task> migrationOperation;
    private readonly ServiceProvider serviceProvider;

    private DatabaseInitializationWorkerHarness(
        ServiceProvider serviceProvider,
        IHostEnvironment environment,
        Func<IServiceProvider, CancellationToken, Task> migrationOperation,
        Func<IServiceProvider, CancellationToken, Task> developmentDataOperation,
        MigrationStoreResolutionProbe? storeProbe = null)
    {
        this.serviceProvider = serviceProvider;
        this.environment = environment;
        this.migrationOperation = migrationOperation;
        this.developmentDataOperation = developmentDataOperation;
        StoreProbe = storeProbe;
    }

    public MigrationStoreResolutionProbe? StoreProbe { get; }

    public static DatabaseInitializationWorkerHarness Create(Func<CancellationToken, Task> initializationOperation)
    {
        return Create(
            Environments.Production,
            initializationOperation,
            static _ => Task.CompletedTask);
    }

    public static DatabaseInitializationWorkerHarness Create(
        string environmentName,
        Func<CancellationToken, Task> migrationOperation,
        Func<CancellationToken, Task> developmentDataOperation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
        ArgumentNullException.ThrowIfNull(migrationOperation);
        ArgumentNullException.ThrowIfNull(developmentDataOperation);

        var services = new ServiceCollection();
        services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var environment = new TestHostEnvironment("ViajantesTurismo.MigrationService.Tests")
        {
            EnvironmentName = environmentName,
        };

        return new DatabaseInitializationWorkerHarness(
            services.BuildServiceProvider(),
            environment,
            (_, ct) => migrationOperation(ct),
            (_, ct) => developmentDataOperation(ct));
    }

    public static DatabaseInitializationWorkerHarness CreateWithDefaultInitialization(string environmentName)
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
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment("ViajantesTurismo.MigrationService.Tests")
        {
            EnvironmentName = environmentName,
        });
        services.AddScoped<IBrandingSettingsStore, EmptyBrandingSettingsStore>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(adminDatabaseName, adminDatabaseRoot);
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped(sp => new DevelopmentDataInitializer(
            sp.GetRequiredService<AdminWriteDbContext>(),
            sp.GetRequiredService<TimeProvider>()));
        var environment = services
            .Where(static descriptor => descriptor.ServiceType == typeof(IHostEnvironment))
            .Select(static descriptor => descriptor.ImplementationInstance)
            .OfType<IHostEnvironment>()
            .Single();

        return new DatabaseInitializationWorkerHarness(
            services.BuildServiceProvider(),
            environment,
            static (_, _) => Task.CompletedTask,
            static (_, _) => Task.CompletedTask,
            probe);
    }

    public DatabaseInitializationWorker CreateWorker()
    {
        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            NullLogger<DatabaseInitializationWorker>.Instance,
            migrationOperation,
            developmentDataOperation);
    }

    public DatabaseInitializationWorker CreateDefaultWorker()
    {
        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            NullLoggerFactory.Instance);
    }

    public DatabaseInitializationWorker CreateDefaultWorker(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            loggerFactory);
    }

    public async Task ShouldContainDevelopmentData(CancellationToken ct)
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

    public async Task ShouldNotContainDevelopmentData(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdminWriteDbContext>();
        var tourCount = await dbContext.Tours.CountAsync(ct);
        var customerCount = await dbContext.Customers.CountAsync(ct);
        var bookingCount = await dbContext.Set<Booking>().CountAsync(ct);

        tourCount.ShouldBe(0);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    public void Dispose()
    {
        serviceProvider.Dispose();
    }
}
