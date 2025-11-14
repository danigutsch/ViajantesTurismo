using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CanCreateBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal RegularBikePrice = 100m;

    [Fact]
    public async Task Can_Create_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("John", "Doe");

        var bookingRequest = new CreateBookingDto
        {
            TourId = tourDto.Id,
            PrincipalCustomerId = customerDto.Id,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null,
            RoomType = RoomTypeDto.SingleRoom,
            Notes = "Test booking"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(tourDto.Id, booking.TourId);
        Assert.Equal(customerDto.Id, booking.CustomerId);
        var expectedPrice = CalculateExpectedPrice(
            basePrice: BaseTourPrice,
            roomSupplement: 0m,
            principalBikePrice: RegularBikePrice);
        Assert.Equal(expectedPrice, booking.TotalPrice);
        Assert.Equal("Test booking", booking.Notes);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Companion()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Jane", "Smith");
        var companionDto = await CreateTestCustomer("Bob", "Smith");

        var bookingRequest = new CreateBookingDto
        {
            TourId = tourDto.Id,
            PrincipalCustomerId = customerDto.Id,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = companionDto.Id,
            CompanionBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.DoubleRoom,
            Notes = "Couple booking"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(companionDto.Id, booking.CompanionId);
        Assert.Contains("Bob Smith", booking.CompanionName);
    }

    [Fact]
    public async Task Create_Booking_Returns_Not_Found_For_Invalid_Tour_Id()
    {
        // Arrange
        var customerDto = await CreateTestCustomer("Test", "User");

        var bookingRequest = new CreateBookingDto
        {
            TourId = Guid.CreateVersion7(),
            PrincipalCustomerId = customerDto.Id,
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null,
            RoomType = RoomTypeDto.SingleRoom
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Booking_Returns_Not_Found_For_Invalid_Customer_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();

        var bookingRequest = new CreateBookingDto
        {
            TourId = tourDto.Id,
            PrincipalCustomerId = Guid.CreateVersion7(),
            PrincipalBikeType = BikeTypeDto.Regular,
            CompanionCustomerId = null,
            CompanionBikeType = null,
            RoomType = RoomTypeDto.SingleRoom
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Percentage_Discount()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Disc", "Percent");

        var bookingRequest = new CreateBookingDto
        {
            TourId = tour.Id,
            PrincipalCustomerId = customer.Id,
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.SingleRoom,
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 15m,
            DiscountReason = "Early bird"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(DiscountTypeDto.Percentage, booking.DiscountType);
        Assert.Equal(15m, booking.DiscountAmount);
        var expectedPrice = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, 15m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task Can_Create_Booking_With_Absolute_Discount()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Disc", "Absolute");

        var bookingRequest = new CreateBookingDto
        {
            TourId = tour.Id,
            PrincipalCustomerId = customer.Id,
            PrincipalBikeType = BikeTypeDto.Regular,
            RoomType = RoomTypeDto.SingleRoom,
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 200m,
            DiscountReason = "VIP discount"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(DiscountTypeDto.Absolute, booking.DiscountType);
        Assert.Equal(200m, booking.DiscountAmount);
        var expectedPrice = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, null, 200m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
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
}
