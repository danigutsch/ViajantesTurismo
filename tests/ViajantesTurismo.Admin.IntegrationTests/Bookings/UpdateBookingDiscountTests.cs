using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class UpdateBookingDiscountTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal RegularBikePrice = 100m;
    private const decimal ValidPercentageDiscount = 10m;
    private const decimal ValidAbsoluteDiscount = 150m;
    private const decimal OverAllowedPercentageDiscount = 150m;
    private const decimal AbsoluteDiscountExceedingSubtotal = 3000m;

    [Fact]
    public async Task Can_Update_Booking_Discount_Percentage()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Perc", "Entage");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = ValidPercentageDiscount,
            DiscountReason = "Seasonal offer"
        };

        // Act
        var updateResponse = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Percentage, updated.DiscountType);
        Assert.Equal(10m, updated.DiscountAmount);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, ValidPercentageDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Can_Update_Booking_Discount_Absolute()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Abs", "Olute");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = ValidAbsoluteDiscount,
            DiscountReason = "VIP customer"
        };

        // Act
        var updateResponse = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(DiscountTypeDto.Absolute, updated.DiscountType);
        Assert.Equal(150m, updated.DiscountAmount);
        var expected = CalculateExpectedPrice(BaseTourPrice, 0m, RegularBikePrice, null, null, ValidAbsoluteDiscount);
        Assert.Equal(expected, updated.TotalPrice);
    }

    [Fact]
    public async Task Update_Booking_Discount_Invalid_Percentage_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Inval", "Percent");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = OverAllowedPercentageDiscount,
            DiscountReason = "Too big"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Booking_Discount_Absolute_ExceedsSubtotal_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Exceed", "Subtotal");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = AbsoluteDiscountExceedingSubtotal,
            DiscountReason = "Impossible"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Discount_On_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Lock", "Discount");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        var updateDto = new UpdateBookingDiscountDto
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 50m,
            DiscountReason = "Late attempt"
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/bookings/{booking.Id}/discount", UriKind.Relative), updateDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
