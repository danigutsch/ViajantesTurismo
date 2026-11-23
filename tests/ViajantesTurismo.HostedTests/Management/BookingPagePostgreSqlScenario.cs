using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore.Authentication;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Admin.Api;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Arranges a migrated Admin database and an authenticated Admin HTTP host.</summary>
internal static class BookingPagePostgreSqlScenario
{
    /// <summary>The number of tours in the fixture.</summary>
    internal const int TourCount = 50;

    /// <summary>The number of bookings seeded for each tour.</summary>
    internal const int BookingsPerTour = 16;

    /// <summary>The Admin API token audience.</summary>
    internal const string Audience = "admin-api";

    /// <summary>Seeds real domain bookings, including all statuses, discounts and optional companions.</summary>
    internal static async Task<IReadOnlyList<Booking>> Seed(string connectionString, CancellationToken ct)
    {
        await using var databaseContext = new AdminWriteDbContextDesignTimeFactory().CreateDbContext([]);
        databaseContext.Database.SetConnectionString(connectionString);
        await databaseContext.Database.MigrateAsync(ct);

        var customers = Enumerable.Range(0, BookingsPerTour + 1)
            .Select(index => EntityBuilders.BuildCustomer(new CustomerOptions(
                FirstName: $"PagingCustomer{index:D2}",
                Email: $"booking-page-{index:D2}@example.test")))
            .ToArray();
        databaseContext.Customers.AddRange(customers);
        var tours = Enumerable.Range(0, TourCount)
            .Select(index => EntityBuilders.BuildTour(new TourOptions(
                Identifier: $"PAGING-{index:D2}",
                Name: $"Paging Tour {index:D2}",
                Pricing: new TourPricingOptions(BasePrice: 800m + (index * 7m)),
                Capacity: new TourCapacityOptions(MaxCustomers: 20))))
            .ToArray();
        var bookingDate = new DateTime(2029, 3, 4, 15, 0, 0, DateTimeKind.Utc);
        foreach (var tour in tours)
        {
            for (var index = 0; index < BookingsPerTour; index++)
            {
                var discount = (index % 3) switch
                {
                    0 => BookingDiscountDefinition.Percentage(12.34m),
                    1 => new BookingDiscountDefinition(DiscountType.Absolute, 13.42m),
                    _ => BookingDiscountDefinition.None,
                };
                var request = index == 0
                    ? TourBookingRequest.CreateDouble(customers[index].Id, BikeType.Regular, customers[^1].Id,
                        BikeType.EBike, RoomType.DoubleOccupancy, discount)
                    : TourBookingRequest.CreateSingle(customers[index].Id, BikeType.Regular, RoomType.SingleOccupancy, discount);
                var booking = tour.AddBooking(request, bookingDate.AddDays(index)).Value;
                switch (index % 4)
                {
                    case 0:
                        tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();
                        break;
                    case 1:
                        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
                        break;
                    case 2:
                        tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
                        tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();
                        break;
                }
            }
        }

        databaseContext.Tours.AddRange(tours);
        await databaseContext.SaveEntities(ct);
        return [.. tours.SelectMany(tour => tour.Bookings)];
    }

    /// <summary>Starts the real Admin API with the test database and authentication, without unrelated workers.</summary>
    internal static WebApplicationFactory<AdminApiHostEntryPoint> Host(
        string connectionString, BookingPageMaterializationProbe materialization) =>
        WebApplicationTestHost.Create<AdminApiHostEntryPoint>(
            environment: Environments.Development,
            configuration: new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = connectionString,
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority,
                [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString,
            },
            configureTestServices: services =>
            {
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                services.RemoveAll<IHostedService>();
                services.ConfigureDbContext<AdminReadDbContext>(options => options.AddInterceptors(materialization));
            });
}
