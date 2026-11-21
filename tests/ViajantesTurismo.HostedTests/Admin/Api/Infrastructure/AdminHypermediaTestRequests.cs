using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;

namespace ViajantesTurismo.HostedTests.Admin.Api.Infrastructure;

/// <summary>Creates independently owned resources for the Admin hypermedia tests.</summary>
internal static class AdminHypermediaTestRequests
{
    /// <summary>Posts a valid fixture to the selected Admin collection.</summary>
    internal static async Task<HttpResponseMessage> Create(HttpClient client, string collection, CancellationToken ct) =>
        collection switch
        {
            "customers" => await client.CreateCustomer(DtoBuilders.BuildCreateCustomerDto(), ct),
            "tours" => await client.CreateTour(DtoBuilders.BuildCreateTourDto(), ct),
            "bookings" => await CreateBooking(client, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(collection), collection, "Unknown Admin collection.")
        };

    private static async Task<HttpResponseMessage> CreateBooking(HttpClient client, CancellationToken ct)
    {
        var customer = await client.CreateTestCustomer("Hypermedia", "Customer", ct);
        var tour = await client.CreateTestTour("hypermedia", "Hypermedia Tour", ct);
        var booking = DtoBuilders.BuildCreateBookingDto(tourId: tour.Id, principalCustomerId: customer.Id);
        return await client.CreateBooking(booking, ct);
    }
}
