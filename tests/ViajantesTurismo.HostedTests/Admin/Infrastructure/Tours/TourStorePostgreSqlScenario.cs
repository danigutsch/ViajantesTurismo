using ViajantesTurismo.Admin.Infrastructure.Tours;
using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;
using ViajantesTurismo.Admin.Infrastructure.Payments;

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

    /// <summary>Loads a tour by ID under a row lock.</summary>
    public Task<Option<Tour>> GetById(Guid tourId, CancellationToken ct) =>
        ReadLockedTour(store => store.GetById(tourId, ct), ct);

    /// <summary>Loads the locked tour containing the booking with <paramref name="bookingId" />.</summary>
    public Task<Option<Tour>> GetByBookingId(Guid bookingId, CancellationToken ct) =>
        ReadLockedTour(store => store.GetByBookingId(bookingId, ct), ct);

    /// <summary>Attempts a locking lookup with no active transaction.</summary>
    public async Task<Option<Tour>> GetByIdWithoutTransaction(Guid tourId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        using var protection = new PaymentProtection(new ConfigurationBuilder().Build());
        var store = new TourCommandStore(dbContext, new TourStore(dbContext, new PaymentHistoryStore(dbContext, protection)));
        return await store.GetById(tourId, ct);
    }

    /// <summary>Exercises the transaction behavior with a confirmation and a supplied outcome after the save.</summary>
    public async Task<Result> ConfirmWithOutcome(Guid bookingId, Func<Result> afterSave, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        using var protection = new PaymentProtection(new ConfigurationBuilder().Build());
        using var services = new ServiceCollection().BuildServiceProvider();
        var behavior = new AdminTransactionBehavior<ConfirmBookingCommand, Result>(
            dbContext,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<AdminTransactionBehavior<ConfirmBookingCommand, Result>>.Instance);
        var store = new TourCommandStore(dbContext, new TourStore(dbContext, new PaymentHistoryStore(dbContext, protection)));
        var handler = new ConfirmBookingCommandHandler(store, dbContext);
        var command = new ConfirmBookingCommand(bookingId);

        return await behavior.Handle(command, async () =>
        {
            var confirmation = await handler.Handle(command, ct);
            return confirmation.IsSuccess ? afterSave() : confirmation;
        }, ct);
    }

    /// <summary>Disposes the test database.</summary>
    public ValueTask DisposeAsync() => database.DisposeAsync();

    private async Task<Option<Tour>> ReadLockedTour(Func<ITourCommandStore, Task<Option<Tour>>> load, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        using var protection = new PaymentProtection(new ConfigurationBuilder().Build());
        return await load(new TourCommandStore(dbContext, new TourStore(dbContext, new PaymentHistoryStore(dbContext, protection))));
    }

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
