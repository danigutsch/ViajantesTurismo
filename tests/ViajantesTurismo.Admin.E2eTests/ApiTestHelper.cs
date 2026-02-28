using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests;

/// <summary>
/// Helpers for creating test data via the API, used by data-mutating E2E tests
/// that need to own their data for parallel safety.
/// </summary>
internal static class ApiTestHelper
{
    public static async Task<GetTourDto> CreateTourAsync(
        HttpClient client,
        int minCustomers = 1,
        int maxCustomers = 20)
    {
        var dto = new CreateTourDto
        {
            Identifier = $"TEST-{Guid.NewGuid():N}",
            Name = $"Test Tour {Guid.NewGuid():N}"[..30],
            StartDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(37),
            Price = 1000m,
            SingleRoomSupplementPrice = 200m,
            RegularBikePrice = 50m,
            EBikePrice = 100m,
            Currency = CurrencyDto.Euro,
            IncludedServices = ["Hotel", "Breakfast"],
            MinCustomers = minCustomers,
            MaxCustomers = maxCustomers
        };

        var response = await client.PostAsJsonAsync("/tours", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetTourDto>())!;
    }

    public static async Task<GetCustomerDto> CreateCustomerAsync(HttpClient client)
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var phone = $"+5511{Random.Shared.Next(10000000, 99999999)}";
        var dto = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = $"Test{uid}",
                LastName = "User",
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
                BikeType = BikeTypeDto.Regular
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
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>())!;
    }

    public static async Task<GetBookingDto> CreateBookingAsync(
        HttpClient client,
        Guid tourId,
        Guid customerId)
    {
        var dto = new CreateBookingDto
        {
            TourId = tourId,
            PrincipalCustomerId = customerId,
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.SingleOccupancy
        };

        var response = await client.PostAsJsonAsync("/bookings", dto);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>())!;
    }

    public static async Task<GetBookingDto> ConfirmBookingAsync(
        HttpClient client,
        Guid bookingId)
    {
        var response = await client.PostAsync(new Uri($"/bookings/{bookingId}/confirm", UriKind.Relative), null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>())!;
    }
}
