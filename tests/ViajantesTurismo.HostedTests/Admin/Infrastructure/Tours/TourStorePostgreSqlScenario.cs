using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

/// <summary>Arranges an isolated PostgreSQL database for tour store tests.</summary>
internal sealed class TourStorePostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private TourStorePostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    /// <summary>Creates a scenario over a database issued by <paramref name="fixture" />.</summary>
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async ValueTask<TourStorePostgreSqlScenario> Create(
        PostgreSqlTestServerFixture fixture,
        CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new TourStorePostgreSqlScenario(database);
    }

    /// <summary>Migrates the database and persists the tour.</summary>
    public async Task Seed(Tour tour, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        dbContext.Tours.Add(tour);
        await dbContext.SaveEntities(ct);
    }

    /// <summary>Looks up the id of the tour containing the booking with <paramref name="bookingId" />.</summary>
    public async Task<Option<Guid>> GetTourIdByBookingId(Guid bookingId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        using var protection = new ViajantesTurismo.Admin.Infrastructure.Payments.PaymentProtection(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        return await new TourStore(dbContext, new ViajantesTurismo.Admin.Infrastructure.Payments.PaymentHistoryStore(dbContext, protection)).GetTourIdByBookingId(bookingId, ct);
    }

    /// <summary>Disposes the test database.</summary>
    public ValueTask DisposeAsync() => database.DisposeAsync();

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddAdminDispositionHolds();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(
            IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AdminWriteDbContext(options, configurations);
    }
}
