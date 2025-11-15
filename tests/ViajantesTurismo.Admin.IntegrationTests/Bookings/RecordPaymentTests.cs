using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class RecordPaymentTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal BaseTourPrice = 2000m;
    private const decimal RegularBikePrice = 100m;
    private const decimal FirstPaymentAmount = 1000m;
    private const decimal PaymentAmountExceedingRemainingBalance = 3000m;

    [Fact]
    public async Task Can_Record_Payment_And_Update_Status_To_PartiallyPaid()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Pay", "Part");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.BankTransfer,
            ReferenceNumber = "REF-500",
            Notes = "Down payment"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
        Assert.Single(updated.Payments);
        Assert.Equal(500m, updated.Payments.First().Amount);
    }

    [Fact]
    public async Task Can_Record_Multiple_Payments_And_Update_Status_To_Paid()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Pay", "Full");
        var booking = await CreateTestBooking(tour.Id, customer.Id);

        var expectedTotal = booking.TotalPrice;
        var secondPaymentAmount = expectedTotal - FirstPaymentAmount;

        var payment1 = new CreatePaymentDto
        {
            Amount = FirstPaymentAmount,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.CreditCard,
            ReferenceNumber = "REF-1"
        };
        var response1 = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), payment1, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var payment2 = new CreatePaymentDto
        {
            Amount = secondPaymentAmount,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.CreditCard,
            ReferenceNumber = "REF-2"
        };
        var response2 = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), payment2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);

        Assert.Equal(PaymentStatusDto.Paid, updated.PaymentStatus);
        Assert.Equal(2, updated.Payments.Count);
        Assert.Equal(expectedTotal, updated.Payments.Sum(p => p.Amount));
    }

    [Fact]
    public async Task Record_Payment_Exceeds_RemainingBalance_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Bad", "Pay");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = PaymentAmountExceedingRemainingBalance,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_Returns_NotFound_For_Invalid_Booking_Id()
    {
        // Arrange
        var paymentDto = new CreatePaymentDto
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash,
            ReferenceNumber = "REF-123",
            Notes = "Test payment"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{Guid.CreateVersion7()}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_With_Negative_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Neg", "Payment");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = -100m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash,
            Notes = "Invalid negative"
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_With_Zero_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await CreateTestTour();
        var customer = await CreateTestCustomer("Zero", "Payment");
        var booking = await CreateTestBooking(tour.Id, customer.Id);
        var paymentDto = new CreatePaymentDto
        {
            Amount = 0m,
            PaymentDate = DateTime.UtcNow.Date,
            Method = PaymentMethodDto.Cash
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_All_Methods_Updates_Status()
    {
        var paymentMethods = new[]
        {
            PaymentMethodDto.CreditCard,
            PaymentMethodDto.BankTransfer,
            PaymentMethodDto.Cash,
            PaymentMethodDto.Check,
            PaymentMethodDto.PayPal,
            PaymentMethodDto.Other
        };

        foreach (var method in paymentMethods)
        {
            // Arrange
            var tour = await CreateTestTour($"CUBA2024-{method}", $"Cuba Adventure 2024 - {method}");
            var customer = await CreateTestCustomer($"Pay{method}", "Test");
            var booking = await CreateTestBooking(tour.Id, customer.Id);

            var paymentDto = new CreatePaymentDto
            {
                Amount = 500m,
                PaymentDate = DateTime.UtcNow.Date,
                Method = method,
                ReferenceNumber = $"REF-{method}",
                Notes = $"Payment via {method}"
            };

            // Act
            var response = await Client.PostAsJsonAsync(new Uri($"/bookings/{booking.Id}/payments", UriKind.Relative), paymentDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var getBooking = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
            var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
            Assert.Single(updated.Payments);
            Assert.Equal(method, updated.Payments.First().Method);
        }
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
