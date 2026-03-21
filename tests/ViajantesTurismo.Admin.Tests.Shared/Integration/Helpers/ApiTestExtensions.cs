using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

/// <summary>
/// Optional inputs used when creating a tour for integration and E2E tests.
/// </summary>
public sealed class CreateTourOptions
{
    /// <summary>
    /// Gets the minimum customer count.
    /// </summary>
    public int MinCustomers { get; init; } = 1;

    /// <summary>
    /// Gets the maximum customer count.
    /// </summary>
    public int MaxCustomers { get; init; } = 20;

    /// <summary>
    /// Gets the tour currency.
    /// </summary>
    public CurrencyDto Currency { get; init; } = CurrencyDto.Euro;

    /// <summary>
    /// Gets the optional tour identifier override.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets the optional tour name override.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the optional tour start date override.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Gets the optional tour end date override.
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Gets the tour base price.
    /// </summary>
    public decimal Price { get; init; } = 1000m;
}

/// <summary>
/// Extension methods for creating and managing test data via the API.
/// Designed for parallel-safe owned-data tests in integration and E2E tests.
/// </summary>
public static class ApiTestExtensions
{
    private static async Task<T> ReadRequiredJson<T>(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus
    )
    {
        if (response.StatusCode != expectedStatus)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Expected HTTP {(int)expectedStatus} ({expectedStatus}) but got " +
                $"{(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
        }

        return await response.Content.ReadFromJsonAsync<T>()
               ?? throw new InvalidOperationException(
                   $"Response body for {typeof(T).Name} was null.");
    }

    /// <summary>
    /// Creates a tour via the API and returns the created <see cref="GetTourDto"/>.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="options">Optional inputs used to customize the created tour.</param>
    /// <returns>The created tour.</returns>
    public static async Task<GetTourDto> CreateTour(this HttpClient client, CreateTourOptions? options = null)
    {
        var resolvedOptions = options ?? new CreateTourOptions();
        var resolvedStartDate = resolvedOptions.StartDate ?? DateTime.UtcNow.AddDays(30);
        var resolvedEndDate = resolvedOptions.EndDate ?? resolvedStartDate.AddDays(7);

        var dto = new CreateTourDto
        {
            Identifier = resolvedOptions.Identifier ?? $"TEST-{Guid.NewGuid():N}",
            Name = resolvedOptions.Name ?? $"Test Tour {Guid.NewGuid():N}"[..30],
            StartDate = resolvedStartDate,
            EndDate = resolvedEndDate,
            Price = resolvedOptions.Price,
            SingleRoomSupplementPrice = 200m,
            RegularBikePrice = 50m,
            EBikePrice = 100m,
            Currency = resolvedOptions.Currency,
            IncludedServices = ["Hotel", "Breakfast"],
            MinCustomers = resolvedOptions.MinCustomers,
            MaxCustomers = resolvedOptions.MaxCustomers
        };

        var response = await client.PostAsJsonAsync("/tours", dto);
        return await response.ReadRequiredJson<GetTourDto>(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates a customer via the API and returns the created <see cref="GetCustomerDto"/>.
    /// </summary>
    public static async Task<GetCustomerDto> CreateCustomer(
        this HttpClient client,
        string? firstName = null,
        string? lastName = null,
        BikeTypeDto bikeType = BikeTypeDto.Regular
    )
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var phone = $"+5511{Random.Shared.Next(10000000, 99999999)}";
        var dto = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = firstName ?? $"Test{uid}",
                LastName = lastName ?? "User",
                BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Other",
                Nationality = "Brazilian",
                Occupation = "Tester"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = uid,
                IdNationality = "BR"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = $"test-{uid}@example.com",
                Mobile = phone,
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "Test Street 1",
                Complement = null,
                Neighborhood = "Centro",
                PostalCode = "00000-000",
                City = "Test City",
                State = "TC",
                Country = "Brazil"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 70m,
                HeightCentimeters = 170,
                BikeType = bikeType
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+5511999999999"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };

        var response = await client.PostAsJsonAsync("/customers", dto);
        return await response.ReadRequiredJson<GetCustomerDto>(HttpStatusCode.Created);
    }

    /// <summary>
    /// Creates a booking via the API and returns the created <see cref="GetBookingDto"/>.
    /// </summary>
    public static async Task<GetBookingDto> CreateBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var dto = new CreateBookingDto
        {
            TourId = tourId,
            PrincipalCustomerId = customerId,
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.SingleOccupancy
        };

        var response = await client.PostAsJsonAsync("/bookings", dto);
        return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.Created);
    }

    /// <summary>
    /// Retrieves a booking by identifier via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="bookingId">The booking identifier.</param>
    /// <returns>The current booking representation.</returns>
    public static async Task<GetBookingDto> GetBooking(this HttpClient client, Guid bookingId)
    {
        var response = await client.GetAsync(new Uri($"/bookings/{bookingId}", UriKind.Relative));
        return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Returns all bookings from the API.
    /// </summary>
    public static async Task<GetBookingDto[]> GetAllBookings(this HttpClient client)
    {
        var response = await client.GetAsync(new Uri("/bookings", UriKind.Relative));
        return await response.ReadRequiredJson<GetBookingDto[]>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Returns all tours from the API.
    /// </summary>
    public static async Task<GetTourDto[]> GetAllTours(this HttpClient client)
    {
        var response = await client.GetAsync(new Uri("/tours", UriKind.Relative));
        return await response.ReadRequiredJson<GetTourDto[]>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Confirms a booking via the API and returns the updated <see cref="GetBookingDto"/>.
    /// </summary>
    public static async Task<GetBookingDto> ConfirmBooking(this HttpClient client, Guid bookingId)
    {
        var response = await client.PostAsync(
            new Uri($"/bookings/{bookingId}/confirm", UriKind.Relative), null);
        return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Creates and confirms a booking via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <returns>The confirmed booking.</returns>
    public static async Task<GetBookingDto> CreateConfirmedBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var booking = await client.CreateBooking(tourId, customerId);
        return await client.ConfirmBooking(booking.Id);
    }

    /// <summary>
    /// Cancels a booking via the API and returns the updated <see cref="GetBookingDto"/>.
    /// </summary>
    public static async Task<GetBookingDto> CancelBooking(this HttpClient client, Guid bookingId)
    {
        var response = await client.PostAsync(
            new Uri($"/bookings/{bookingId}/cancel", UriKind.Relative), null);
        return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Creates and cancels a booking via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <returns>The cancelled booking.</returns>
    public static async Task<GetBookingDto> CreateCancelledBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var booking = await client.CreateBooking(tourId, customerId);
        return await client.CancelBooking(booking.Id);
    }

    /// <summary>
    /// Completes a booking via the API and returns the updated <see cref="GetBookingDto"/>.
    /// </summary>
    public static async Task<GetBookingDto> CompleteBooking(this HttpClient client, Guid bookingId)
    {
        var response = await client.PostAsync(
            new Uri($"/bookings/{bookingId}/complete", UriKind.Relative), null);
        return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.OK);
    }

    /// <summary>
    /// Creates, confirms, and completes a booking via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <returns>The completed booking.</returns>
    public static async Task<GetBookingDto> CreateCompletedBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var booking = await client.CreateConfirmedBooking(tourId, customerId);
        return await client.CompleteBooking(booking.Id);
    }

    /// <summary>
    /// Records a payment for a booking via the API.
    /// </summary>
    public static async Task RecordPayment(this HttpClient client, Guid bookingId, decimal amount)
    {
        var dto = new CreatePaymentDto
        {
            Amount = amount,
            PaymentDate = DateTime.UtcNow,
            Method = PaymentMethodDto.CreditCard,
            Notes = "E2E test payment"
        };

        var response = await client.PostAsJsonAsync(
            new Uri($"/bookings/{bookingId}/payments", UriKind.Relative), dto);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Creates a booking and records a partial payment via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <param name="amount">The partial payment amount to record.</param>
    /// <returns>The updated partially paid booking.</returns>
    public static async Task<GetBookingDto> CreatePartiallyPaidBooking(this HttpClient client, Guid tourId, Guid customerId, decimal amount)
    {
        var booking = await client.CreateBooking(tourId, customerId);
        await client.RecordPayment(booking.Id, amount);
        return await client.GetBooking(booking.Id);
    }

    /// <summary>
    /// Creates and fully pays a booking via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <returns>The updated fully paid booking.</returns>
    public static async Task<GetBookingDto> CreatePaidBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var booking = await client.CreateBooking(tourId, customerId);
        await client.RecordPayment(booking.Id, booking.TotalPrice);
        return await client.GetBooking(booking.Id);
    }

    /// <summary>
    /// Creates, confirms, and fully pays a booking via the API.
    /// </summary>
    /// <param name="client">The API client.</param>
    /// <param name="tourId">The owning tour identifier.</param>
    /// <param name="customerId">The principal customer identifier.</param>
    /// <returns>The updated confirmed and fully paid booking.</returns>
    public static async Task<GetBookingDto> CreateConfirmedPaidBooking(this HttpClient client, Guid tourId, Guid customerId)
    {
        var booking = await client.CreateConfirmedBooking(tourId, customerId);
        await client.RecordPayment(booking.Id, booking.TotalPrice);
        return await client.GetBooking(booking.Id);
    }
}
