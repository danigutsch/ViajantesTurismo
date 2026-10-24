using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.UnitTests.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Seeds aggregates through an in-memory write context and reads them back through the real query service.
/// </summary>
internal sealed class QueryServiceReadModelScenario : IAsyncDisposable
{
    private readonly AdminWriteDbContext writeContext;
    private readonly AdminReadDbContext readContext;
    private readonly PaymentProtection protection;

    private QueryServiceReadModelScenario()
    {
        var root = new InMemoryDatabaseRoot();
        var databaseName = Guid.CreateVersion7().ToString("N");
        // The write context has no outbox configuration here, so keep its model out of EF Core's shared
        // in-memory model cache; otherwise later in-memory tests reuse a model without the outbox.
        writeContext = new AdminWriteDbContext(new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .EnableServiceProviderCaching(false)
            .Options);
        readContext = new AdminReadDbContext(new DbContextOptionsBuilder<AdminReadDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .EnableServiceProviderCaching(false)
            .Options);
        protection = new PaymentProtection(new ConfigurationBuilder().Build());
        QueryService = new QueryService(readContext, new PaymentHistoryStore(writeContext, protection));
    }

    public IQueryService QueryService
    {
        get;
    }

    public static QueryServiceReadModelScenario Create() => new();

    public async Task<Booking> AddTourWithSingleBooking(Tour tour, CancellationToken ct)
    {
        var customer = EntityIdTestData.CreateCustomer();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, new SingleBookingOptions(CustomerId: customer.Id));
        bookingResult.IsSuccess.ShouldBeTrue("Failed to add booking for query service setup.");
        var booking = bookingResult.Value;
        writeContext.Customers.Add(customer);
        writeContext.Tours.Add(tour);
        await writeContext.SaveChangesAsync(ct);
        return booking;
    }

    public async Task Save(CancellationToken ct) => await writeContext.SaveChangesAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await readContext.DisposeAsync();
        await writeContext.DisposeAsync();
        protection.Dispose();
    }
}
