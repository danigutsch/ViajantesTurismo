using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.Admin.UnitTests.Domain;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class DevelopmentDataInitializerScenario : IAsyncDisposable
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);
    private readonly DevelopmentDataInitializer initializer;
    private readonly ServiceProvider provider;

    private DevelopmentDataInitializerScenario(ServiceProvider provider)
    {
        this.provider = provider;
        initializer = provider.GetRequiredService<DevelopmentDataInitializer>();
    }

    private AdminWriteDbContext DbContext => provider.GetRequiredService<AdminWriteDbContext>();

    public static DevelopmentDataInitializerScenario Create()
    {
        var services = new ServiceCollection();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddDbContext<AdminWriteDbContext>(options =>
        {
            options.UseInMemoryDatabase(Guid.CreateVersion7().ToString("N"));
            services.ApplyDbContextOptionConfigurations<AdminWriteDbContext>(options);
        });
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(CurrentTime));
        services.AddScoped(sp => new DevelopmentDataInitializer(
            sp.GetRequiredService<AdminWriteDbContext>(),
            sp.GetRequiredService<TimeProvider>()));

        return new DevelopmentDataInitializerScenario(services.BuildServiceProvider());
    }

    public Task Initialize(CancellationToken ct) => initializer.Initialize(ct);

    public async Task AddExistingTour(CancellationToken ct)
    {
        var tour = Tour.Create(new TourDefinition(
            "EXIST001",
            "Existing Tour",
            new TourScheduleDefinition(CurrentTime.UtcDateTime.AddDays(1), CurrentTime.UtcDateTime.AddDays(7)),
            new TourPricingDefinition(1000m, 100m, 100m, 200m, Currency.Real),
            new TourCapacityDefinition(1, 10),
            ["Hotel"])).Value;
        DbContext.Tours.Add(tour);
        await DbContext.SaveChangesAsync(ct);
    }

    public async Task KeepOnlyOneBaselineCustomer(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        var tours = await DbContext.Tours.ToArrayAsync(ct);
        var customers = await DbContext.Customers.OrderBy(static customer => customer.Id).ToArrayAsync(ct);
        DbContext.RemoveRange(bookings);
        DbContext.RemoveRange(tours);
        DbContext.RemoveRange(customers.Skip(1));
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task AddUnrecognizedCustomer(CancellationToken ct)
    {
        DbContext.Customers.Add(EntityIdTestData.CreateCustomer());
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

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

    public async Task ShouldContainExpectedBookingStatuses(CancellationToken ct)
    {
        var tours = await DbContext.Tours
            .Include(static tour => tour.Bookings)
            .OrderBy(static tour => tour.Identifier)
            .ToArrayAsync(ct);

        tours.Select(static tour => tour.Identifier).ShouldBe(new[] { "CITY001", "CULT001", "FOWI003", "HIST002", "NATR001" });
        tours[0].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(new[] { BookingStatus.Confirmed, BookingStatus.Confirmed, BookingStatus.Cancelled }.Order().ToArray());
        tours[1].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(new[] { BookingStatus.Confirmed, BookingStatus.Confirmed });
        tours[2].Bookings.Select(static booking => booking.Status).ToArray().ShouldBe(new[] { BookingStatus.Pending });
        tours[3].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(new[] { BookingStatus.Pending, BookingStatus.Confirmed }.Order().ToArray());
        tours[4].Bookings.Select(static booking => booking.Status).Order().ToArray().ShouldBe(new[] { BookingStatus.Confirmed, BookingStatus.Completed }.Order().ToArray());
    }

    public async Task ShouldContainOnlyTours(int expectedTourCount, CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(expectedTourCount);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    public async Task ShouldContainExistingTourAndDevelopmentData(CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(6);
        customerCount.ShouldBe(15);
        bookingCount.ShouldBe(10);
    }

    public async Task ShouldNotContainDevelopmentData(CancellationToken ct)
    {
        var tourCount = await DbContext.Tours.CountAsync(ct);
        var customerCount = await DbContext.Customers.CountAsync(ct);
        var bookingCount = await DbContext.Tours.SelectMany(tour => tour.Bookings).CountAsync(ct);

        tourCount.ShouldBe(0);
        customerCount.ShouldBe(0);
        bookingCount.ShouldBe(0);
    }

    public async Task KeepOnlyBaselineTours(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        var customers = await DbContext.Customers.ToArrayAsync(ct);
        DbContext.RemoveRange(bookings);
        DbContext.RemoveRange(customers);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task AlterBaselineTourPrice(CancellationToken ct)
    {
        var tour = await DbContext.Tours.OrderBy(static item => item.Identifier).FirstAsync(ct);
        var result = tour.UpdateBasePrice(tour.Pricing.BasePrice + 1m);
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task AlterBaselineTourSchedulePrecision(CancellationToken ct)
    {
        var tour = await DbContext.Tours.OrderBy(static item => item.Identifier).FirstAsync(ct);
        var result = tour.UpdateSchedule(tour.Schedule.StartDate, tour.Schedule.EndDate.AddTicks(1));
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task RemoveBaselineBookings(CancellationToken ct)
    {
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        DbContext.RemoveRange(bookings);
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task ResetBaselineBookingsToPendingCheckpoint(CancellationToken ct)
    {
        var payments = await DbContext.Set<Payment>().ToArrayAsync(ct);
        var bookings = await DbContext.Set<Booking>().ToArrayAsync(ct);
        DbContext.RemoveRange(payments);
        foreach (var booking in bookings)
        {
            DbContext.Entry(booking)
                .Property(static item => item.Status)
                .CurrentValue = BookingStatus.Pending;
        }

        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

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
            notes: "Arbitrary pending booking"));
        result.IsSuccess.ShouldBeTrue();
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

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

    public async Task AlterPendingBookingBasePrice(CancellationToken ct)
    {
        var booking = await DbContext.Set<Booking>().OrderBy(static item => item.Id).FirstAsync(ct);
        DbContext.Entry(booking)
            .Property(static item => item.BasePrice)
            .CurrentValue = booking.BasePrice + 1m;
        await DbContext.SaveChangesAsync(ct);
        DbContext.ChangeTracker.Clear();
    }

    public async Task ShouldContainPendingBookingCheckpoint(CancellationToken ct)
    {
        DbContext.ChangeTracker.Clear();
        var bookings = await DbContext.Set<Booking>()
            .Include(static booking => booking.Payments)
            .AsNoTracking()
            .ToArrayAsync(ct);

        bookings.Length.ShouldBe(10);
        bookings.Count(static booking => booking.Status == BookingStatus.Pending).ShouldBe(10);
        bookings.Sum(static booking => booking.Payments.Count).ShouldBe(0);
    }

    public async Task ShouldContainExpectedSeedBookingStates(CancellationToken ct)
    {
        DbContext.ChangeTracker.Clear();
        var customers = await DbContext.Customers
            .AsNoTracking()
            .OrderBy(static customer => customer.Id)
            .ToArrayAsync(ct);
        var bookings = await DbContext.Set<Booking>()
            .Include(static booking => booking.Payments)
            .AsNoTracking()
            .ToArrayAsync(ct);
        var bookingsByCustomer = bookings.ToDictionary(
            static booking => booking.PrincipalCustomer.CustomerId);
        var expected = new (Guid CustomerId, BookingStatus Status, PaymentStatus PaymentStatus, decimal PaidRatio)[]
        {
            (customers[0].Id, BookingStatus.Confirmed, PaymentStatus.Paid, 1m),
            (customers[1].Id, BookingStatus.Confirmed, PaymentStatus.PartiallyPaid, 0.5m),
            (customers[2].Id, BookingStatus.Pending, PaymentStatus.PartiallyPaid, 0.25m),
            (customers[3].Id, BookingStatus.Confirmed, PaymentStatus.Paid, 1m),
            (customers[5].Id, BookingStatus.Completed, PaymentStatus.Unpaid, 0m),
            (customers[6].Id, BookingStatus.Cancelled, PaymentStatus.Unpaid, 0m),
            (customers[7].Id, BookingStatus.Confirmed, PaymentStatus.PartiallyPaid, 0.75m),
            (customers[9].Id, BookingStatus.Pending, PaymentStatus.Unpaid, 0m),
            (customers[4].Id, BookingStatus.Confirmed, PaymentStatus.Paid, 1m),
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
            .Include(static booking => booking.Payments)
            .AsNoTracking()
            .OrderBy(static booking => booking.Id)
            .ToArrayAsync(ct);

        return bookings
            .Select(static booking => (
                booking.Id,
                booking.Status,
                booking.PaymentStatus,
                booking.Payments.Sum(static payment => payment.Amount),
                booking.Discount.Type,
                booking.Discount.Amount,
                booking.Discount.Reason))
            .ToArray();
    }

    public async Task<Guid[]> GetTourIds(CancellationToken ct) =>
        await DbContext.Tours
            .OrderBy(static tour => tour.Id)
            .Select(static tour => tour.Id)
            .ToArrayAsync(ct);

    public async Task<Guid[]> GetCustomerIds(CancellationToken ct) =>
        await DbContext.Customers
            .OrderBy(static customer => customer.Id)
            .Select(static customer => customer.Id)
            .ToArrayAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await provider.DisposeAsync();
    }
}
