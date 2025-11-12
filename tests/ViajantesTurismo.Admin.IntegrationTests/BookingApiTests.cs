using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests;

[Collection("Api collection")]
public sealed class BookingApiTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceScope _scope;

    public BookingApiTests(ApiFixture fixture)
    {
        _client = fixture.CreateClient();

        using var scope = fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = new Seeder(dbContext);
        seeder.Seed();

        _scope = fixture.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _dbContext.Dispose();
        _scope.Dispose();
    }

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
        var response = await _client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking =
            await response.Content.ReadFromJsonAsync<GetBookingDto>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(tourDto.Id, booking.TourId);
        Assert.Equal(customerDto.Id, booking.CustomerId);
        var expectedPrice = CalculateExpectedPrice(
            basePrice: 2000m,
            roomSupplement: 0m,
            principalBikePrice: 100m);
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
        var response = await _client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking =
            await response.Content.ReadFromJsonAsync<GetBookingDto>(
                cancellationToken: TestContext.Current.CancellationToken);
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
        var response = await _client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Get_All_Bookings()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customer1 = await CreateTestCustomer("Alice", "Johnson");
        var customer2 = await CreateTestCustomer("Charlie", "Brown");

        await CreateTestBooking(tourDto.Id, customer1.Id, null);
        await CreateTestBooking(tourDto.Id, customer2.Id, null);

        // Act
        var response =
            await _client.GetAsync(new Uri("/bookings", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.True(bookings.Length >= 2);
    }

    [Fact]
    public async Task Can_Get_Booking_By_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("David", "Wilson");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id, null);

        // Act
        var response = await _client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking =
            await response.Content.ReadFromJsonAsync<GetBookingDto>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(booking);
        Assert.Equal(createdBooking.Id, booking.Id);
        var expectedPrice = CalculateExpectedPrice(
            basePrice: 2000m,
            roomSupplement: 0m,
            principalBikePrice: 100m);
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Fact]
    public async Task Get_Booking_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Act
        var response = await _client.GetAsync(new Uri("/bookings/99999", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Get_Bookings_By_Tour_Id()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customer1 = await CreateTestCustomer("Emma", "Davis");
        var customer2 = await CreateTestCustomer("Frank", "Miller");

        await CreateTestBooking(tourDto.Id, customer1.Id, null);
        await CreateTestBooking(tourDto.Id, customer2.Id, null);

        // Act
        var response = await _client.GetAsync(new Uri($"/bookings/tour/{tourDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(tourDto.Id, b.TourId));
    }

    [Fact]
    public async Task Can_Get_Bookings_By_Customer_Id()
    {
        // Arrange
        var tour1 = await CreateTestTour();
        var tour2 = await CreateTestTour("CUBA2025", "Cuba Adventure 2025");
        var customerDto = await CreateTestCustomer("Grace", "Lee");

        await CreateTestBooking(tour1.Id, customerDto.Id, null);
        await CreateTestBooking(tour2.Id, customerDto.Id, null);

        // Act
        var response = await _client.GetAsync(new Uri($"/bookings/customer/{customerDto.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(customerDto.Id, b.CustomerId));
    }

    [Fact]
    public async Task Get_Bookings_By_Customer_Id_Includes_Bookings_As_Companion()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var primaryCustomer = await CreateTestCustomer("Henry", "Taylor");
        var companionCustomer = await CreateTestCustomer("Iris", "Anderson");

        await CreateTestBooking(tourDto.Id, primaryCustomer.Id, companionCustomer.Id);

        // Act
        var response = await _client.GetAsync(new Uri($"/bookings/customer/{companionCustomer.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Single(bookings);
        Assert.Equal(companionCustomer.Id, bookings[0].CompanionId);
    }

    [Fact]
    public async Task Can_Update_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Jack", "Martin");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id, null);

        var notesRequest = new UpdateBookingNotesDto { Notes = "Updated notes" };
        var notesResponse = await _client.PatchAsJsonAsync(
            new Uri($"/bookings/{createdBooking.Id}/notes", UriKind.Relative),
            notesRequest,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, notesResponse.StatusCode);

        var confirmResponse = await _client.PostAsync(
            new Uri($"/bookings/{createdBooking.Id}/confirm", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);

        var getResponse = await _client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var updatedBooking =
            await getResponse.Content.ReadFromJsonAsync<GetBookingDto>(
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal("Updated notes", updatedBooking.Notes);
        Assert.Equal(BookingStatusDto.Confirmed, updatedBooking.Status);
    }

    [Fact]
    public async Task Update_Booking_Notes_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var updateRequest = new UpdateBookingNotesDto
        {
            Notes = "Test"
        };

        // Act
        var response = await _client.PatchAsJsonAsync(new Uri("/bookings/99999/notes", UriKind.Relative), updateRequest,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Delete_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Kate", "White");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id, null);

        // Act
        var response = await _client.DeleteAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Act
        var response = await _client.DeleteAsync(new Uri("/bookings/99999", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Cancel_Booking()
    {
        // Arrange
        var tourDto = await CreateTestTour();
        var customerDto = await CreateTestCustomer("Laura", "Brown");
        var createdBooking = await CreateTestBooking(tourDto.Id, customerDto.Id, null);

        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await _client.PostAsync(
            new Uri($"/bookings/{createdBooking.Id}/cancel", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync(
            new Uri($"/bookings/{createdBooking.Id}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        getResponse.EnsureSuccessStatusCode();
        var updatedBooking =
            await getResponse.Content.ReadFromJsonAsync<GetBookingDto>(
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatusDto.Cancelled, updatedBooking.Status);
        Assert.Equal(createdBooking.TotalPrice, updatedBooking.TotalPrice);
        Assert.Equal(createdBooking.PaymentStatus, updatedBooking.PaymentStatus);
    }

    [Fact]
    public async Task Cancel_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Act
        var response = await _client.PostAsync(
            new Uri("/bookings/99999/cancel", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

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
            Price = 2000.00m,
            DoubleRoomSupplementPrice = 500.00m,
            RegularBikePrice = 100.00m,
            EBikePrice = 200.00m,
            MinCustomers = 4,
            MaxCustomers = 12,
            Currency = CurrencyDto.UsDollar,
            IncludedServices = ["Hotel", "Breakfast", "Bike"]
        };

        var response = await _client.PostAsJsonAsync(new Uri("/tours", UriKind.Relative), tourRequest,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        var getResponse = await _client.GetAsync(location, TestContext.Current.CancellationToken);
        return (await getResponse.Content.ReadFromJsonAsync<GetTourDto>(
            cancellationToken: TestContext.Current.CancellationToken))!;
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

        var response = await _client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), customerRequest,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetCustomerDto>(
            cancellationToken: TestContext.Current.CancellationToken))!;
    }

    private async Task<GetBookingDto> CreateTestBooking(Guid tourId, Guid customerId, Guid? companionId)
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

        var response = await _client.PostAsJsonAsync(new Uri("/bookings", UriKind.Relative), bookingRequest,
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GetBookingDto>(
            cancellationToken: TestContext.Current.CancellationToken))!;
    }
}
