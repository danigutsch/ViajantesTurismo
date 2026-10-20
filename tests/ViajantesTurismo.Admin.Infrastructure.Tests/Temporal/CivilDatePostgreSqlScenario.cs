using System.Diagnostics.CodeAnalysis;
using ViajantesTurismo.Admin.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Temporal;

internal sealed class CivilDatePostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;

    private CivilDatePostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async Task<CivilDatePostgreSqlScenario> Create(PostgreSqlTestServerFixture fixture, CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new CivilDatePostgreSqlScenario(database);
    }

    public async Task Seed(CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(ct);
        var customer = EntityBuilders.BuildCustomer(null);
        customer.UpdatePersonalInfo(PersonalInfo.Create("Ana", "Silva", "Female", new DateOnly(1990, 1, 1),
            "Brazilian", "Engineer", new DateOnly(2030, 2, 3)).Value);
        var tour = EntityBuilders.BuildTour();
        tour.UpdateSchedule(new DateOnly(2030, 2, 3), new DateOnly(2030, 2, 10)).IsSuccess.ShouldBeTrue();
        var now = new DateTimeOffset(2030, 2, 4, 14, 0, 0, TimeSpan.Zero);
        var booking = tour.AddBooking(TourBookingRequest.CreateSingle(customer.Id, BikeType.Regular,
            RoomType.DoubleOccupancy, BookingDiscountDefinition.None), now.UtcDateTime).Value;
        tour.RecordBookingPayment(booking.Id, 100m, new DateOnly(2030, 2, 3), PaymentMethod.Cash,
            new FakeTimeProvider(now)).IsSuccess.ShouldBeTrue();
        context.Customers.Add(customer);
        context.Tours.Add(tour);
        await context.SaveEntities(ct);
    }

    public async Task Migrate(bool forward, string timeZone, CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('TimeZone', {timeZone}, false)", ct);
        await context.Database.GetService<IMigrator>().MigrateAsync(
            forward ? null : "20260724210254_InitialAdmin", ct);
    }

    public async Task SetLegacyDates(CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE "Tours" SET "StartDate" = TIMESTAMPTZ '2030-02-03T00:30:00Z',
                "EndDate" = TIMESTAMPTZ '2030-02-10T23:30:00Z';
            UPDATE "CustomerPersonalInfo" SET "BirthDate" = TIMESTAMPTZ '1990-01-01T00:30:00Z';
            UPDATE "Payment" SET "PaymentDate" = TIMESTAMPTZ '2030-02-03T23:30:00Z';
            """, ct);
    }

    public async Task<(DateOnly BirthDate, DateOnly StartDate, DateOnly EndDate, DateOnly PaymentDate,
        DateTime BookingDate, DateTime RecordedAt)> Read(CancellationToken ct)
    {
        await using var context = CreateContext();
        var customer = await context.Customers.AsNoTracking().SingleAsync(ct);
        var tour = await context.Tours.Include(item => item.Bookings).ThenInclude(item => item.Payments)
            .AsNoTracking().SingleAsync(ct);
        var booking = tour.Bookings.ShouldHaveSingleItem();
        var payment = booking.Payments.ShouldHaveSingleItem();
        return (customer.PersonalInfo.BirthDate, tour.Schedule.StartDate, tour.Schedule.EndDate,
            payment.PaymentDate, booking.BookingDate, payment.RecordedAt);
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    private AdminWriteDbContext CreateContext()
    {
        var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(database.ConnectionString);
        return context;
    }
}
