using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>In-memory scenario that arranges and inspects database state around the development data initializer.</summary>
internal sealed class DevelopmentDataInitializerScenario : IAsyncDisposable
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] ExpectedTourIdentifiers = ["CITY001", "CULT001", "FOWI003", "HIST002", "NATR001"];
    private static readonly BookingStatus[] CityBookingStatuses = [BookingStatus.Confirmed, BookingStatus.Confirmed, BookingStatus.Cancelled];
    private static readonly BookingStatus[] CultureBookingStatuses = [BookingStatus.Confirmed, BookingStatus.Confirmed];
    private static readonly BookingStatus[] FoodAndWineBookingStatuses = [BookingStatus.Pending];
    private static readonly BookingStatus[] HistoryBookingStatuses = [BookingStatus.Pending, BookingStatus.Confirmed];
    private static readonly BookingStatus[] NatureBookingStatuses = [BookingStatus.Confirmed, BookingStatus.Completed];
    private readonly DevelopmentDataInitializer initializer;
    private readonly ServiceProvider provider;

    private DevelopmentDataInitializerScenario(ServiceProvider provider)
    {
        this.provider = provider;
        initializer = provider.GetRequiredService<DevelopmentDataInitializer>();
    }

    private AdminWriteDbContext DbContext => provider.GetRequiredService<AdminWriteDbContext>();

    /// <summary>Creates a scenario with an in-memory write context and a fixed clock.</summary>
    public static DevelopmentDataInitializerScenario Create()
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.CreateVersion7().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options, enableDevelopmentDiagnostics: false);
        });
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(CurrentTime));
        services.AddScoped(sp => new DevelopmentDataInitializer(
            sp.GetRequiredService<AdminWriteDbContext>(),
            sp.GetRequiredService<TimeProvider>()));

        return new DevelopmentDataInitializerScenario(services.BuildServiceProvider());
    }

    /// <summary>Runs the development data initializer.</summary>
    public Task Initialize(CancellationToken ct) => initializer.Initialize(ct);

    /// <summary>Adds a tour that is not part of the development seed.</summary>
    public async Task AddExistingTour(CancellationToken ct)
    {
        var tour = Tour.Create(new TourDefinition(
            "EXIST001",
            "Existing Tour",
            new TourScheduleDefinition(DateOnly.FromDateTime(CurrentTime.UtcDateTime.AddDays(1)), DateOnly.FromDateTime(CurrentTime.UtcDateTime.AddDays(7))),
            new TourPricingDefinition(1000m, 100m, 100m, 200m, Currency.Real),
            new TourCapacityDefinition(1, 10),
            ["Hotel"])).Value;
        DbContext.Tours.Add(tour);
        await DbContext.SaveChangesAsync(ct);
    }

    /// <summary>Removes all bookings and tours and every customer but the first, leaving a partial seed.</summary>
    public async Task KeepOnlyOneBaselineCustomer(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        var tours = await DbContext.Tours.ToArrayAsync(ct);
        var customers = await DbContext.Customers.OrderBy(static customer => customer.Id).ToArrayAsync(ct);
        // This InMemory-only scenario deliberately constructs a partial synthetic seed checkpoint.
        DbContext.RemoveRange(bookings);
        DbContext.RemoveRange(tours);
        DbContext.RemoveRange(customers.Skip(1));
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Adds a customer that is not part of the development seed.</summary>
    public async Task AddUnrecognizedCustomer(CancellationToken ct)
    {
        DbContext.Customers.Add(EntityIdTestData.CreateCustomer());
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Asserts the full development seed: 5 tours, 15 customers, 10 bookings and no documents.</summary>
    public async Task ShouldContainDevelopmentData(CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);
        var documentLineageCount = await DbContext.DocumentLineages.CountAsync(ct);
        var documentDraftCount = await DbContext.DocumentDrafts.CountAsync(ct);
        var documentAuditCount = await DbContext.DocumentAuditRecords.CountAsync(ct);

        tourCount.ShouldBe(5);
        customerCount.ShouldBe(15);
        bookingCount.ShouldBe(10);
        documentLineageCount.ShouldBe(0);
        documentDraftCount.ShouldBe(0);
        documentAuditCount.ShouldBe(0);
    }

    /// <summary>Asserts the seeded tour identifiers and the booking statuses of each tour.</summary>
    public async Task ShouldContainExpectedBookingStatuses(CancellationToken ct)
    {
        var tours = await DbContext.Tours
            .Include(static tour => tour.Bookings)
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);

        tours.Select(static tour => tour.Identifier).ShouldBe(ExpectedTourIdentifiers);
        tours[0].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(CityBookingStatuses);
        tours[1].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(CultureBookingStatuses);
        tours[2].Bookings.Select(static booking => booking.Status).ToArray().ShouldBe(FoodAndWineBookingStatuses);
        tours[3].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(HistoryBookingStatuses);
        tours[4].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(NatureBookingStatuses);
    }

    /// <summary>Asserts the expected tour count with no customers or bookings.</summary>
    public async Task ShouldContainOnlyTours(int expectedTourCount, CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(expectedTourCount);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    /// <summary>Asserts there are no tours, customers or bookings.</summary>
    public async Task ShouldNotContainDevelopmentData(CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(0);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    /// <summary>Removes all bookings and customers, leaving only tours.</summary>
    public async Task KeepOnlyBaselineTours(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        var customers = await DbContext.Customers.ToArrayAsync(ct);
        // This InMemory-only scenario deliberately constructs a partial synthetic seed checkpoint.
        DbContext.RemoveRange(bookings);
        DbContext.RemoveRange(customers);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Raises the base price of the first tour by one.</summary>
    public async Task AlterBaselineTourPrice(CancellationToken ct)
    {
        var tour = await DbContext.Tours.OrderBy(static item => item.Identifier).FirstAsync(ct);
        var result = tour.UpdateBasePrice(tour.Pricing.BasePrice + 1m);
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Removes all bookings, leaving a partial seed.</summary>
    public async Task RemoveBaselineBookings(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        // This InMemory-only scenario deliberately constructs a partial synthetic seed checkpoint.
        DbContext.RemoveRange(bookings);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Sets every booking status to pending.</summary>
    public async Task ResetBaselineBookingsToPendingCheckpoint(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        foreach (var booking in bookings)
        {
            DbContext.Entry(booking)
                .Property(static item => item.Status)
                .CurrentValue = BookingStatus.Pending;
        }

        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Replaces one seeded booking with a different pending booking for the same customer.</summary>
    public async Task ReplaceBaselineBookingWithArbitraryPendingBooking(CancellationToken ct)
    {
        var tours = await DbContext.Tours
            .Include(static tour => tour.Bookings)
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        var customers = await DbContext.Customers
            .OrderBy(static customer => customer.Id)
            .ToArrayAsync(ct);
        var bookingToReplace = tours[3].Bookings.ShouldHaveSingleItem(
            booking => booking.PrincipalCustomer.CustomerId == customers[9].Id);
        DbContext.Remove(bookingToReplace);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();

        var tour = await DbContext.Tours
            .Include(static item => item.Bookings)
            .SingleAsync(item => item.Id == tours[3].Id, ct);
        var result = tour.AddBooking(TourBookingRequest.CreateSingle(
            customers[9].Id,
            customers[9].PhysicalInfo.BikeType,
            customers[9].AccommodationPreferences.RoomType,
            notes: "Arbitrary pending booking"), new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Applies a manual percentage discount to the first seeded booking.</summary>
    public async Task UpdateFirstBaselineBookingDiscount(CancellationToken ct)
    {
        var tours = await DbContext.Tours
            .Include(static tour => tour.Bookings)
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);
        var customers = await DbContext.Customers
            .OrderBy(static customer => customer.Id)
            .ToArrayAsync(ct);
        var booking = tours[0].Bookings.ShouldHaveSingleItem(
            item => item.PrincipalCustomer.CustomerId == customers[0].Id);
        var result = tours[0].UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "Manual checkpoint discount");
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Raises the base price of the first booking by one.</summary>
    public async Task AlterPendingBookingBasePrice(CancellationToken ct)
    {
        var booking = await DbContext.Set<Booking>().OrderBy(static item => item.Id).FirstAsync(ct);
        DbContext.Entry(booking)
            .Property(static item => item.BasePrice)
            .CurrentValue = booking.BasePrice + 1m;
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    /// <summary>Asserts that all 10 bookings are pending.</summary>
    public async Task ShouldContainPendingBookingCheckpoint(CancellationToken ct)
    {
        DbContext.ChangeTracker.Clear();
        var bookings = await DbContext.Set<Booking>()
            .AsNoTracking()
            .ToArrayAsync(ct);

        bookings.Length.ShouldBe(10);
        bookings.Count(static booking => booking.Status == BookingStatus.Pending).ShouldBe(10);
    }

    /// <summary>Asserts the status, payment status and paid ratio of each seeded booking by customer.</summary>
    public async Task ShouldContainExpectedDevelopmentBookingStates(CancellationToken ct)
    {
        DbContext.ChangeTracker.Clear();
        var customers = await DbContext.Customers
            .AsNoTracking()
            .OrderBy(static customer => customer.Id)
            .ToArrayAsync(ct);
        var bookings = await DbContext.Set<Booking>()
            .AsNoTracking()
            .ToArrayAsync(ct);
        var bookingsByCustomer = bookings.ToDictionary(
            static booking => booking.PrincipalCustomer.CustomerId);
        var expected = new (Guid CustomerId, BookingStatus Status, PaymentStatus PaymentStatus, decimal PaidRatio)[]
        {
            (customers[0].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m),
            (customers[1].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m),
            (customers[2].Id, BookingStatus.Pending, PaymentStatus.Unpaid, 0m),
            (customers[3].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m),
            (customers[5].Id, BookingStatus.Completed, PaymentStatus.Unpaid, 0m),
            (customers[6].Id, BookingStatus.Cancelled, PaymentStatus.Unpaid, 0m),
            (customers[7].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m),
            (customers[9].Id, BookingStatus.Pending, PaymentStatus.Unpaid, 0m),
            (customers[4].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m),
            (customers[8].Id, BookingStatus.Confirmed, PaymentStatus.Unpaid, 0m)
        };
        var actual = expected
            .Select(item =>
            {
                var booking = bookingsByCustomer[item.CustomerId];
                var paidRatio = booking.AmountPaid / booking.TotalPrice;
                return (item.CustomerId, booking.Status, booking.PaymentStatus, paidRatio);
            })
            .ToArray();

        actual.ShouldBe(expected);
    }

    /// <summary>Returns the status, payment and discount state of every booking ordered by id.</summary>
    public async Task<(
        Guid BookingId,
        BookingStatus Status,
        PaymentStatus PaymentStatus,
        decimal AmountPaid,
        DiscountType DiscountType,
        decimal DiscountAmount,
        string? DiscountReason)[]>
        GetBookingStates(
        CancellationToken ct)
    {
        DbContext.ChangeTracker.Clear();
        var bookings = await DbContext.Set<Booking>()
            .AsNoTracking()
            .OrderBy(static booking => booking.Id)
            .ToArrayAsync(ct);

        return [.. bookings
            .Select(static booking => (
                booking.Id,
                booking.Status,
                booking.PaymentStatus,
                booking.AmountPaid,
                booking.Discount.Type,
                booking.Discount.Amount,
                booking.Discount.Reason))];
    }

    /// <summary>Returns the tour ids in ascending order.</summary>
    public async Task<Guid[]> GetTourIds(CancellationToken ct) =>
        await DbContext.Tours
            .OrderBy(static tour => tour.Id)
            .Select(static tour => tour.Id)
            .ToArrayAsync(ct);

    /// <summary>Returns the customer ids in ascending order.</summary>
    public async Task<Guid[]> GetCustomerIds(CancellationToken ct) =>
        await DbContext.Customers
            .OrderBy(static customer => customer.Id)
            .Select(static customer => customer.Id)
            .ToArrayAsync(ct);

    /// <summary>Disposes the service provider.</summary>
    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
    }
}
