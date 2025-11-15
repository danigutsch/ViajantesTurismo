using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CancelBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal RegularBikePrice = 100m;

    [Fact]
    public async Task Can_Cancel_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Laura", "Brown");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id);

        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await Client.PostAsync(new Uri($"/bookings/{createdBooking.Id}/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelledBooking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatusDto.Cancelled, cancelledBooking.Status);
        Assert.Equal(createdBooking.TotalPrice, cancelledBooking.TotalPrice);
        Assert.Equal(createdBooking.PaymentStatus, cancelledBooking.PaymentStatus);
    }

    [Fact]
    public async Task Cancel_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Act
        var response = await Client.PostAsync(new Uri("/bookings/99999/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Completed_Booking_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Complete", "Then Cancel");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var confirmResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/confirm", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var completeResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/complete", UriKind.Relative), null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Act
        var cancelResponse = await Client.PostAsync(new Uri($"/bookings/{booking.Id}/cancel", UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
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
