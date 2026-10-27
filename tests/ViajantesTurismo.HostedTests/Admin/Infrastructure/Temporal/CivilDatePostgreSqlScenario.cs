using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using ViajantesTurismo.Admin.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Temporal;

/// <summary>Arranges a PostgreSQL database for verifying that civil dates survive timezone changes and migrations.</summary>
internal sealed class CivilDatePostgreSqlScenario : IAsyncDisposable
{
    private readonly PostgreSqlTestDatabase database;

    private CivilDatePostgreSqlScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
    }

    /// <summary>Creates a scenario over a database issued by <paramref name="fixture" />.</summary>
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async Task<CivilDatePostgreSqlScenario> Create(PostgreSqlTestServerFixture fixture, CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new CivilDatePostgreSqlScenario(database);
    }

    /// <summary>Migrates the database and seeds one customer born 1990-01-01 and one tour from 2030-02-03 to 2030-02-10 with a booking.</summary>
    public async Task Seed(CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync(ct);
        var customer = EntityBuilders.BuildCustomer(new CustomerOptions(
            FirstName: "Ana",
            LastName: "Silva",
            Gender: "Female",
            BirthDate: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Nationality: "Brazilian",
            Occupation: "Engineer"));
        var tour = EntityBuilders.BuildTour();
        tour.UpdateSchedule(new DateOnly(2030, 2, 3), new DateOnly(2030, 2, 10)).IsSuccess.ShouldBeTrue();
        var now = new DateTimeOffset(2030, 2, 4, 14, 0, 0, TimeSpan.Zero);
        var booking = tour.AddBooking(TourBookingRequest.CreateSingle(customer.Id, BikeType.Regular,
            RoomType.DoubleOccupancy, BookingDiscountDefinition.None), now.UtcDateTime).Value;
        context.Customers.Add(customer);
        context.Tours.Add(tour);
        await context.SaveEntities(ct);
    }

    /// <summary>Migrates forward, or back to the initial migration when <paramref name="forward" /> is false, with the session time zone set to <paramref name="timeZone" />.</summary>
    public async Task Migrate(bool forward, string timeZone, CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.OpenConnectionAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('TimeZone', {timeZone}, false)", ct);
        await context.Database.GetService<IMigrator>().MigrateAsync(
            forward ? null : "20260724210254_InitialAdmin", ct);
    }

    /// <summary>Overwrites tour and birth dates with timestamptz values near midnight UTC.</summary>
    public async Task SetTimestampDates(CancellationToken ct)
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE "Tours" SET "StartDate" = TIMESTAMPTZ '2030-02-03T00:30:00Z',
                "EndDate" = TIMESTAMPTZ '2030-02-10T23:30:00Z';
            UPDATE "CustomerPersonalInfo" SET "BirthDate" = TIMESTAMPTZ '1990-01-01T00:30:00Z';
            """, ct);
    }

    /// <summary>Reads back the customer birth date, tour start and end dates, and the booking date.</summary>
    public async Task<(DateOnly BirthDate, DateOnly StartDate, DateOnly EndDate,
        DateTime BookingDate)> Read(CancellationToken ct)
    {
        await using var context = CreateContext();
        var customer = await context.Customers.AsNoTracking().SingleAsync(ct);
        var tour = await context.Tours.Include(item => item.Bookings)
            .AsNoTracking().SingleAsync(ct);
        var booking = tour.Bookings.ShouldHaveSingleItem();
        return (customer.PersonalInfo.BirthDate, tour.Schedule.StartDate, tour.Schedule.EndDate,
            booking.BookingDate);
    }

    /// <summary>Disposes the test database.</summary>
    public ValueTask DisposeAsync() => database.DisposeAsync();

    private AdminWriteDbContext CreateContext()
    {
        var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(database.ConnectionString);
        return context;
    }
}
