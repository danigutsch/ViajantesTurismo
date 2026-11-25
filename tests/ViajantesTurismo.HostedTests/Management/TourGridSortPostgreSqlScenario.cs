using Microsoft.EntityFrameworkCore;
using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Seeds tours with independent sort keys and a mix of booking statuses.</summary>
internal static class TourGridSortPostgreSqlScenario
{
    /// <summary>Returns the tours and the full tour with confirmed, pending, cancelled and completed bookings.</summary>
    internal static async Task<(IReadOnlyList<Tour> Tours, Tour Full)> Seed(string connectionString, CancellationToken ct)
    {
        const int tourCount = 23;
        string[] names = ["Bela Tour", "Arcus Tour", "Cristal Tour"];
        Currency[] currencies = [Currency.Eur, Currency.Usd, Currency.Brl];
        var date = new DateTime(2029, 5, 3, 0, 0, 0, DateTimeKind.Utc);
        var tours = Enumerable.Range(0, tourCount).Select(index =>
        {
            var start = date.AddDays(index * 7 % 5);
            return EntityBuilders.BuildTour(new TourOptions(
                Identifier: $"SORT-TOUR-{tourCount - index:D2}",
                Name: $"{names[index % names.Length]} {index:D2}",
                Schedule: new TourScheduleOptions(start, start.AddDays(AdminDomainLimits.MinimumTourDurationDays + index % 7)),
                Pricing: new TourPricingOptions(BasePrice: 210m + index * 7 % 11, Currency: currencies[index % currencies.Length]),
                Capacity: new TourCapacityOptions(MinCustomers: 2, MaxCustomers: 3)));
        }).ToArray();
        var customers = Enumerable.Range(0, 6).Select(index =>
            EntityBuilders.BuildCustomer(new CustomerOptions(Email: $"tour-grid-sort-{index}@example.test"))).ToArray();
        var full = tours[4];
        var bookingDate = date.AddDays(-14);
        var completed = AddBooking(full, TourBookingRequest.CreateSingle(customers[0].Id, BikeType.Regular, RoomType.DoubleOccupancy), bookingDate);
        var cancelled = AddBooking(full, TourBookingRequest.CreateSingle(customers[1].Id, BikeType.Regular, RoomType.DoubleOccupancy), bookingDate);
        _ = AddBooking(full, TourBookingRequest.CreateSingle(customers[2].Id, BikeType.Regular, RoomType.DoubleOccupancy), bookingDate);
        var confirmed = AddBooking(full, TourBookingRequest.CreateSingle(customers[3].Id, BikeType.Regular, RoomType.DoubleOccupancy), bookingDate);
        var paired = AddBooking(full,
            TourBookingRequest.CreateDouble(customers[4].Id, BikeType.Regular, customers[5].Id, BikeType.Regular, RoomType.DoubleOccupancy), bookingDate);
        full.ConfirmBooking(completed).IsSuccess.ShouldBeTrue();
        full.CompleteBooking(completed).IsSuccess.ShouldBeTrue();
        full.CancelBooking(cancelled).IsSuccess.ShouldBeTrue();
        full.ConfirmBooking(confirmed).IsSuccess.ShouldBeTrue();
        full.ConfirmBooking(paired).IsSuccess.ShouldBeTrue();

        await using var context = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString(connectionString);
        await context.Database.MigrateAsync(ct);
        context.Customers.AddRange(customers);
        context.Tours.AddRange(tours);
        await context.SaveEntities(ct);
        return (tours, full);
    }

    private static Guid AddBooking(Tour tour, TourBookingRequest request, DateTime date)
    {
        var outcome = tour.AddBooking(request, date);
        outcome.IsSuccess.ShouldBeTrue();
        return outcome.Value.Id;
    }
}
