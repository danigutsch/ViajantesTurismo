using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for creating test fixtures in integration tests.
/// These methods combine DTO builders with API helpers to create complete test entities.
/// </summary>
internal static class TestFixtureHelpers
{
    /// <summary>
    /// Creates a test tour and returns the created tour DTO.
    /// </summary>
    public static async Task<GetTourDto> CreateTestTour(
        this HttpClient client,
        string identifier = "CUBA2024",
        string name = "Cuba Adventure 2024",
        CancellationToken cancellationToken = default)
    {
        var tourRequest = DtoBuilders.BuildCreateTourDto(identifier: identifier, name: name);
        var response = await client.CreateTourAsync(tourRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await client.GetAsync(location, cancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(cancellationToken))!;
    }

    /// <summary>
    /// Creates a test customer and returns the created customer DTO.
    /// </summary>
    public static async Task<GetCustomerDto> CreateTestCustomer(
        this HttpClient client,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default)
    {
        var customerRequest = DtoBuilders.BuildCreateCustomerDto(
            firstName: firstName,
            lastName: lastName,
            email: TestDataGenerator.UniqueEmail($"{firstName}.{lastName}".ToLower()),
            nationalId: TestDataGenerator.UniqueNationalId($"{firstName}{lastName}"));

        var response = await client.CreateCustomerAsync(customerRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(cancellationToken))!;
    }

    /// <summary>
    /// Creates a test booking and returns the created booking DTO.
    /// </summary>
    public static async Task<GetBookingDto> CreateTestBooking(
        this HttpClient client,
        Guid tourId,
        Guid customerId,
        Guid? companionId = null,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourId,
            principalCustomerId: customerId,
            companionCustomerId: companionId,
            companionBikeType: companionId.HasValue ? BikeTypeDto.Regular : null,
            roomType: companionId.HasValue ? RoomTypeDto.DoubleRoom : RoomTypeDto.SingleRoom);

        var response = await client.CreateBooking(bookingRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }
}
