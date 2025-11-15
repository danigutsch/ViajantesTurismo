using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingDetailsTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal DoubleRoomSupplement = 500m;
    private const decimal RegularBikePrice = 100m;

    [Fact]
    public async Task Can_Update_Booking_Details_Add_Companion_And_RoomSupplement()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Prim", "Ary");
        var companion = await CreateTestCustomer("Comp", "Anion");
        var booking = await CreateTestBooking(tour.Id, principal.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.DoubleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companion.Id,
            CompanionBikeType = BikeTypeDto.Regular
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(companion.Id, updated.CompanionId);
        var expected = CalculateExpectedPrice(BaseTourPrice, DoubleRoomSupplement, RegularBikePrice, RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Details_Remove_Companion_And_Switch_To_Single()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Switch", "Down");
        var companion = await CreateTestCustomer("To", "Single");
        var booking = await CreateTestBooking(tour.Id, principal.Id, companion.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Null(updated.CompanionId);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Details_SingleRoomWithCompanion_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var principal = await CreateTestCustomer("Bad", "Combo");
        var companion = await CreateTestCustomer("Wrong", "Room");
        var booking = await CreateTestBooking(tour.Id, principal.Id);

        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companion.Id,
            CompanionBikeType = BikeTypeDto.Regular
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Details_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var updateDto = new UpdateBookingDetailsDto
        {
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/details", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            DoubleRoomSupplementPrice = DoubleRoomSupplement,
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

    private static decimal CalculateExpectedPrice(
        decimal basePrice,
        decimal roomSupplement,
        decimal principalBikePrice,
        decimal? companionBikePrice = null,
        decimal? discountPercentage = null,
        decimal? absoluteDiscount = null)
    {
        var totalPrice = basePrice + roomSupplement + principalBikePrice;

        if (companionBikePrice.HasValue)
        {
            totalPrice += companionBikePrice.Value;
        }

        if (discountPercentage.HasValue)
        {
            totalPrice -= totalPrice * (discountPercentage.Value / 100m);
        }

        if (absoluteDiscount.HasValue)
        {
            totalPrice -= absoluteDiscount.Value;
        }

        return totalPrice;
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
