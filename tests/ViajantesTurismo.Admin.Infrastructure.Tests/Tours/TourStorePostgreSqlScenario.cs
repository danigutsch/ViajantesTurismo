using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Tours;

internal sealed class TourStorePostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private TourStorePostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

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

    public async Task Seed(Tour tour, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
        dbContext.Tours.Add(tour);
        await dbContext.SaveEntities(ct);
    }

    public async Task<Option<Guid>> GetTourIdByBookingId(Guid bookingId, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await new TourStore(dbContext).GetTourIdByBookingId(bookingId, ct);
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
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
