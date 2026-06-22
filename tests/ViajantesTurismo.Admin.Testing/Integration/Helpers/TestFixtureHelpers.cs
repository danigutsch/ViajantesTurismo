using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Testing.Builders;

namespace ViajantesTurismo.Admin.Testing.Integration.Helpers;

/// <summary>
/// Helper methods for creating test fixtures in integration tests.
/// These methods combine DTO builders with API helpers to create complete test entities.
/// </summary>
public static class TestFixtureHelpers
{
    /// <summary>
    /// Creates a test tour and returns the created tour DTO.
    /// </summary>
    public static Task<GetTourDto> CreateTestTour(
        this HttpClient client,
        string? identifier = null,
        string? name = null) =>
        CreateTestTour(client, identifier, name, CancellationToken.None);

    /// <summary>
    /// Creates a test tour and returns the created tour DTO.
    /// </summary>
    public static async Task<GetTourDto> CreateTestTour(
        this HttpClient client,
        string? identifier,
        string? name,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        // Ensure identifier and name uniqueness for parallel test safety
        if (identifier is not null || name is not null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

            if (identifier is not null)
            {
                identifier = $"{identifier}-{suffix}";
            }

            if (name is not null)
            {
                name = $"{name} {suffix}";
            }
        }

        var tourRequest = DtoBuilders.BuildCreateTourDto(identifier: identifier, name: name);
        var response = await client.CreateTour(tourRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Failed to create test tour '{identifier}'. " +
                $"Status: {response.StatusCode}, Error: {errorContent}");
        }

        var location = response.Headers.Location;
        var getResponse = await client.GetAsync(location, ct);
        var createdTour = await getResponse.Content.ReadFromJsonAsync<GetTourDto>(ct);

        return createdTour ?? throw new InvalidOperationException("The created tour response body was empty.");
    }

    /// <summary>
    /// Creates a test customer and returns the created customer DTO.
    /// </summary>
    public static Task<GetCustomerDto> CreateTestCustomer(
        this HttpClient client,
        string firstName,
        string lastName) =>
        CreateTestCustomer(client, firstName, lastName, CancellationToken.None);

    /// <summary>
    /// Creates a test customer and returns the created customer DTO.
    /// </summary>
    public static async Task<GetCustomerDto> CreateTestCustomer(
        this HttpClient client,
        string firstName,
        string lastName,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);

        var customerRequest = DtoBuilders.BuildCreateCustomerDto(
            firstName: firstName,
            lastName: lastName,
            email: TestDataGenerator.UniqueEmail($"{firstName}.{lastName}".ToUpperInvariant()),
            nationalId: TestDataGenerator.UniqueNationalId($"{firstName}{lastName}"));

        var response = await client.CreateCustomer(customerRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Failed to create test customer {firstName} {lastName}. " +
                $"Status: {response.StatusCode}, Error: {errorContent}");
        }

        var createdCustomer = await response.Content.ReadFromJsonAsync<GetCustomerDto>(ct);

        return createdCustomer ?? throw new InvalidOperationException("The created customer response body was empty.");
    }

    /// <summary>
    /// Creates a test booking and returns the created booking DTO.
    /// </summary>
    public static Task<GetBookingDto> CreateTestBooking(
        this HttpClient client,
        Guid tourId,
        Guid customerId,
        Guid? companionId = null) =>
        CreateTestBooking(client, tourId, customerId, companionId, CancellationToken.None);

    /// <summary>
    /// Creates a test booking and returns the created booking DTO.
    /// </summary>
    public static async Task<GetBookingDto> CreateTestBooking(
        this HttpClient client,
        Guid tourId,
        Guid customerId,
        Guid? companionId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourId,
            principalCustomerId: customerId,
            companionCustomerId: companionId,
            companionBikeType: companionId.HasValue ? BikeTypeDto.Regular : null,
            roomType: RoomTypeDto.DoubleOccupancy);

        var response = await client.CreateBooking(bookingRequest, ct);
        response.EnsureSuccessStatusCode();
        var createdBooking = await response.Content.ReadFromJsonAsync<GetBookingDto>(ct);

        return createdBooking ?? throw new InvalidOperationException("The created booking response body was empty.");
    }
}
