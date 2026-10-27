using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using PandaBear.EntityFrameworkCore;
using PandaBear.Branding;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Management.Security;
using ViajantesTurismo.MigrationService;

namespace ViajantesTurismo.Admin.UnitTests.MigrationService;

/// <summary>
/// Builds a database initialization worker over in-memory stores and stubbed operations for tests.
/// </summary>
internal sealed class DatabaseInitializationWorkerHarness : IDisposable
{
    private readonly ActivitySource activitySource = new(DatabaseInitializationWorker.ActivitySourceName);
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

    /// <summary>
    /// Gets the probe recording which stores the default initialization resolved, or <see langword="null"/> for custom operations.
    /// </summary>
    public MigrationStoreResolutionProbe? StoreProbe
    {
        get;
    }

    /// <summary>
    /// Gets the activity source passed to workers created by this harness.
    /// </summary>
    public ActivitySource ActivitySource => activitySource;

    /// <summary>
    /// Creates a production-environment harness whose migration runs <paramref name="initializationOperation"/> and development data is a no-op.
    /// </summary>
    public static DatabaseInitializationWorkerHarness Create(Func<CancellationToken, Task> initializationOperation)
    {
        return Create(
            Environments.Production,
            initializationOperation,
            static _ => Task.CompletedTask);
    }

    /// <summary>
    /// Creates a harness for <paramref name="environmentName"/> with the given migration and development data operations.
    /// </summary>
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

    /// <summary>
    /// Creates a harness for <paramref name="environmentName"/> that uses the default initialization over in-memory stores.
    /// </summary>
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
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options, enableDevelopmentDiagnostics: false);
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

    /// <summary>
    /// Creates a worker using the harness operations and activity source.
    /// </summary>
    public DatabaseInitializationWorker CreateWorker()
    {
        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            NullLogger<DatabaseInitializationWorker>.Instance,
            migrationOperation,
            developmentDataOperation,
            activitySource);
    }

    /// <summary>
    /// Creates a worker with the default initialization and a null logger factory.
    /// </summary>
    public DatabaseInitializationWorker CreateDefaultWorker()
    {
        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Creates a worker with the default initialization and <paramref name="loggerFactory"/>.
    /// </summary>
    public DatabaseInitializationWorker CreateDefaultWorker(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new DatabaseInitializationWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            environment,
            loggerFactory);
    }

    /// <summary>
    /// Asserts the seeded development data exists: 5 tours, 15 customers and 10 bookings.
    /// </summary>
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

    /// <summary>
    /// Asserts no tours, customers or bookings exist.
    /// </summary>
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

    /// <summary>
    /// Disposes the service provider and the activity source.
    /// </summary>
    public void Dispose()
    {
        try
        {
            serviceProvider.Dispose();
        }
        finally
        {
            activitySource.Dispose();
        }
    }
}
