using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Seeds a migrated Admin database for the active-tour option search.</summary>
internal static class TourOptionPostgreSqlScenario
{
    /// <summary>Creates a large active collection and matching full and retired tours.</summary>
    internal static async Task<(IReadOnlyList<Tour> Active, Tour Full, Tour Retired)> Seed(
        string connectionString, int total, CancellationToken ct)
    {
        await using var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(connectionString);
        await context.Database.MigrateAsync(ct);

        var start = new DateTime(2029, 5, 3, 0, 0, 0, DateTimeKind.Utc);
        var schedule = new TourScheduleOptions(StartDate: start);
        var active = Enumerable.Range(0, total - 1).Select(index => EntityBuilders.BuildTour(new TourOptions(
            Identifier: $"OPTION-{index:D3}",
            Name: $"Choice {index:D3}",
            Schedule: schedule))).ToList();
        var full = EntityBuilders.BuildTour(new TourOptions(
            Identifier: "ÁGUAS-ONLY",
            Name: "Serra Águas",
            Schedule: schedule,
            Pricing: new TourPricingOptions(BasePrice: 432.10m),
            Capacity: new TourCapacityOptions(MinCustomers: 1, MaxCustomers: 1)));
        var customer = EntityBuilders.BuildCustomer(new CustomerOptions(Email: "tour-option-full@example.test"));
        var booking = full.AddBooking(
            TourBookingRequest.CreateSingle(customer.Id, BikeType.Regular, RoomType.DoubleOccupancy), start.AddDays(-14));
        booking.IsSuccess.ShouldBeTrue();
        full.ConfirmBooking(booking.Value.Id).IsSuccess.ShouldBeTrue();
        active.Add(full);

        var retired = EntityBuilders.BuildTour(new TourOptions(
            Identifier: "ÁGUAS-RETIRED",
            Name: "Serra Águas Retired",
            Schedule: schedule));
        retired.Retire(Guid.NewGuid(), "tour-option-reader", new DateTimeOffset(start.AddDays(-7))).IsSuccess.ShouldBeTrue();

        context.Customers.Add(customer);
        context.Tours.AddRange(active);
        context.Tours.Add(retired);
        await context.SaveEntities(ct);
        return (active, full, retired);
    }
}
