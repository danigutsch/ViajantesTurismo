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
    public static async Task<GetTourDto> CreateTestTour(
        this HttpClient client,
        string? identifier,
        string? name,
        CancellationToken cancellationToken)
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
        var response = await client.CreateTour(tourRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to create test tour '{identifier}'. " +
                $"Status: {response.StatusCode}, Error: {errorContent}");
        }

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
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);

        var customerRequest = DtoBuilders.BuildCreateCustomerDto(
            firstName: firstName,
            lastName: lastName,
            email: TestDataGenerator.UniqueEmail($"{firstName}.{lastName}".ToUpperInvariant()),
            nationalId: TestDataGenerator.UniqueNationalId($"{firstName}{lastName}"));

        var response = await client.CreateCustomer(customerRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to create test customer {firstName} {lastName}. " +
                $"Status: {response.StatusCode}, Error: {errorContent}");
        }

        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(cancellationToken))!;
    }

    /// <summary>
    /// Creates a test booking and returns the created booking DTO.
    /// </summary>
    public static async Task<GetBookingDto> CreateTestBooking(
        this HttpClient client,
        Guid tourId,
        Guid customerId,
        Guid? companionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tourId,
            principalCustomerId: customerId,
            companionCustomerId: companionId,
            companionBikeType: companionId.HasValue ? BikeTypeDto.Regular : null,
            roomType: RoomTypeDto.DoubleOccupancy);

        var response = await client.CreateBooking(bookingRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken))!;
    }
}
