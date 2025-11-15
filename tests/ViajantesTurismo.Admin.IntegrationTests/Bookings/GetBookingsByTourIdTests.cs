using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetBookingsByTourIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal RegularBikePrice = 100m;

    [Fact]
    public async Task Can_Get_Bookings_By_Tour_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customer1 = await CreateTestCustomer("Emma", "Davis");
        var customer2 = await CreateTestCustomer("Frank", "Miller");

        await CreateTestBooking(tourDto.Id, customer1.Id);
        await CreateTestBooking(tourDto.Id, customer2.Id);

        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{tourDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(tourDto.Id, b.TourId));
    }

    [Fact]
    public async Task Get_Bookings_By_Tour_Id_Returns_Empty_For_Invalid_Tour()
    {
        // Act
        var response = await Client.GetAsync(new Uri($"/bookings/tour/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    private async Task<GetTourDto> CreateTestTour(string identifier = "CUBA2024", string name = "Cuba Adventure 2024")
    {
        var tourRequest = new CreateTourDto
        {
            Identifier = identifier,
            Name = name,
            StartDate = DateTime.UtcNow.AddMonths(2),
            EndDate = DateTime.UtcNow.AddMonths(2).AddDays(10),
            Price = BaseTourPrice,
            DoubleRoomSupplementPrice = 500m,
            RegularBikePrice = RegularBikePrice,
            EBikePrice = 200.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel", "Breakfast", "Bike"]
        };

        var response = await Client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), tourRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await Client.GetAsync(location, TestContext.Current.CancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(TestContext.Current.CancellationToken))!;
    }

    private async Task<GetCustomerDto> CreateTestCustomer(string firstName, string lastName)
    {
        var customerRequest = new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = firstName,
                LastName = lastName,
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = $"{firstName}{lastName}{Random.Shared.Next(1000, 9999)}",
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                Mobile = $"+1555{Random.Shared.Next(1000000, 9999999)}",
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "12345",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 75.0m,
                HeightCentimeters = 180,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.SingleRoom,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };

        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), customerRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(TestContext.Current.CancellationToken))!;
    }

    private async Task<GetBookingDto> CreateTestBooking(Guid tourId, Guid customerId, Guid? companionId = null)
    {
        var bookingRequest = new CreateBookingDto
        {
            TourId = tourId,
            PrincipalCustomerId = customerId,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companionId,
            CompanionBikeType = companionId.HasValue ? BikeTypeDto.Regular : null,
            RoomType = companionId.HasValue ? RoomTypeDto.DoubleRoom : RoomTypeDto.SingleRoom,
            Notes = "Test booking"
        };

        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken))!;
    }
}
