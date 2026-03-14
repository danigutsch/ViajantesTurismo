using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;

/// <summary>
/// Helpers for creating test data via the API, used by data-mutating E2E tests
/// that need to own their data for parallel safety.
/// </summary>
internal static class ApiTestExtensions
{
    private static async Task<T> ReadRequiredJson<T>(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        if (response.StatusCode != expectedStatus)
        {
            Assert.Fail($"Expected HTTP {(int)expectedStatus} ({expectedStatus}) but got {(int)response.StatusCode} ({response.StatusCode}).");
        }

        var model = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(model);

        return model;
    }

    extension(HttpClient client)
    {
        public async Task<GetTourDto> CreateTour(int minCustomers = 1,
            int maxCustomers = 20,
            CurrencyDto currency = CurrencyDto.Euro,
            string? identifier = null,
            string? name = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal price = 1000m
        )
        {
            var resolvedStartDate = startDate ?? DateTime.UtcNow.AddDays(30);
            var resolvedEndDate = endDate ?? resolvedStartDate.AddDays(7);

            var dto = new CreateTourDto
            {
                Identifier = identifier ?? $"TEST-{Guid.NewGuid():N}",
                Name = name ?? $"Test Tour {Guid.NewGuid():N}"[..30],
                StartDate = resolvedStartDate,
                EndDate = resolvedEndDate,
                Price = price,
                SingleRoomSupplementPrice = 200m,
                RegularBikePrice = 50m,
                EBikePrice = 100m,
                Currency = currency,
                IncludedServices = ["Hotel", "Breakfast"],
                MinCustomers = minCustomers,
                MaxCustomers = maxCustomers
            };

            var response = await client.PostAsJsonAsync("/tours", dto);
            return await response.ReadRequiredJson<GetTourDto>(HttpStatusCode.Created);
        }

        public async Task<GetCustomerDto> CreateCustomer(
            string? firstName = null,
            string? lastName = null,
            BikeTypeDto bikeType = BikeTypeDto.Regular)
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

        public async Task<GetBookingDto> CreateBooking(Guid tourId,
            Guid customerId
        )
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

        public async Task<GetBookingDto[]> GetAllBookings()
        {
            var response = await client.GetAsync(new Uri("/bookings", UriKind.Relative));
            return await response.ReadRequiredJson<GetBookingDto[]>(HttpStatusCode.OK);
        }

        public async Task<GetTourDto[]> GetAllTours()
        {
            var response = await client.GetAsync(new Uri("/tours", UriKind.Relative));
            return await response.ReadRequiredJson<GetTourDto[]>(HttpStatusCode.OK);
        }

        public async Task<GetBookingDto> ConfirmBooking(Guid bookingId
        )
        {
            var response = await client.PostAsync(new Uri($"/bookings/{bookingId}/confirm", UriKind.Relative), null);
            return await response.ReadRequiredJson<GetBookingDto>(HttpStatusCode.OK);
        }

        public async Task RecordPayment(Guid bookingId, decimal amount)
        {
            var dto = new CreatePaymentDto
            {
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                Method = PaymentMethodDto.CreditCard,
                Notes = "E2E test payment"
            };

            var response = await client.PostAsJsonAsync(new Uri($"/bookings/{bookingId}/payments", UriKind.Relative), dto);
            response.EnsureSuccessStatusCode();
        }
    }
}
