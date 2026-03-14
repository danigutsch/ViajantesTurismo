using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class UpdateCustomerTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Customer()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Alice", "Johnson", TestContext.Current.CancellationToken);
        var updatedEmail = TestDataGenerator.UniqueEmail("alice.smith");

        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: "Alice",
            lastName: "Johnson-Smith",
            occupation: "Senior Designer",
            email: updatedEmail,
            mobile: "+447123456789",
            instagram: "@alicesmith",
            facebook: "alice.smith",
            street: "456 Baker St",
            complement: "Flat 3B",
            postalCode: "NW1 6XE",
            city: "London",
            state: "England",
            country: "UK",
            weightKg: 62.0m,
            heightCentimeters: 170,
            bikeType: BikeTypeDto.EBike,
            roomType: RoomTypeDto.SingleOccupancy,
            bedType: BedTypeDto.DoubleBed,
            emergencyContactName: "Robert Johnson",
            emergencyContactMobile: "+447987654321",
            allergies: "Lactose",
            medicalAdditionalInfo: "Prefers vegetarian meals");

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var updatedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(updateRequest.PersonalInfo.LastName, updatedCustomer.PersonalInfo.LastName);
        Assert.Equal(updateRequest.PersonalInfo.Occupation, updatedCustomer.PersonalInfo.Occupation);
        Assert.Equal(updateRequest.ContactInfo.Email, updatedCustomer.ContactInfo.Email);
        Assert.Equal(updateRequest.ContactInfo.Instagram, updatedCustomer.ContactInfo.Instagram);
        Assert.Equal(updateRequest.Address.Street, updatedCustomer.Address.Street);
        Assert.Equal(updateRequest.Address.Complement, updatedCustomer.Address.Complement);
        Assert.Equal(updateRequest.PhysicalInfo.WeightKg, updatedCustomer.PhysicalInfo.WeightKg);
        Assert.Equal(updateRequest.PhysicalInfo.BikeType, updatedCustomer.PhysicalInfo.BikeType);
        Assert.Equal(updateRequest.AccommodationPreferences.RoomType, updatedCustomer.AccommodationPreferences.RoomType);
        Assert.Equal(updateRequest.MedicalInfo.Allergies, updatedCustomer.MedicalInfo.Allergies);
        Assert.Equal(updateRequest.MedicalInfo.AdditionalInfo, updatedCustomer.MedicalInfo.AdditionalInfo);
    }

    [Fact]
    public async Task Update_Customer_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = 999999;
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: "Test",
            lastName: "User",
            occupation: "Tester");

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Customer_With_Empty_FirstName()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Alice", "Johnson", TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(firstName: "Alice", lastName: "Johnson");
        updateRequest = updateRequest with
        {
            PersonalInfo = updateRequest.PersonalInfo with { FirstName = "" }
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Customer_With_Empty_Email()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Alice", "Johnson", TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(firstName: "Alice", lastName: "Johnson");
        updateRequest = updateRequest with
        {
            ContactInfo = updateRequest.ContactInfo with { Email = "" }
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Update_Customer_With_Future_BirthDate()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Alice", "Johnson", TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(firstName: "Alice", lastName: "Johnson");
        updateRequest = updateRequest with
        {
            PersonalInfo = updateRequest.PersonalInfo with { BirthDate = DateTime.UtcNow.AddDays(1) }
        };

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Customer_Email()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Bob", "Wilson", TestContext.Current.CancellationToken);
        var originalEmail = customer.Email;
        var newEmail = TestDataGenerator.UniqueEmail("bob.wilson.new");

        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: customer.FirstName,
            lastName: customer.LastName,
            email: newEmail);

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCustomer);
        Assert.NotEqual(originalEmail, updatedCustomer.ContactInfo.Email);
        Assert.Equal(newEmail, updatedCustomer.ContactInfo.Email);
    }

    [Fact]
    public async Task Cannot_Update_Customer_With_Invalid_Email_Format()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Charlie", "Brown", TestContext.Current.CancellationToken);
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(firstName: "Charlie", lastName: "Brown", email: "not-a-valid-email");

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Can_Update_Customer_Multiple_Times_With_Same_Data()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Diana", "Smith", TestContext.Current.CancellationToken);
        var updatedEmail = TestDataGenerator.UniqueEmail("diana.smithjones");
        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: "Diana",
            lastName: "Smith-Jones",
            occupation: "Architect",
            email: updatedEmail);

        // Act
        var firstResponse = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);
        var secondResponse = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);

        var getResponse = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(updateRequest.PersonalInfo.LastName, updatedCustomer.PersonalInfo.LastName);
        Assert.Equal(updateRequest.ContactInfo.Email, updatedCustomer.ContactInfo.Email);
    }

    [Fact]
    public async Task Can_Update_Customer_That_Has_Bookings()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Emily", "Johnson", TestContext.Current.CancellationToken);
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var updatedEmail = TestDataGenerator.UniqueEmail("emily.williams");

        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: "Emily",
            lastName: "Johnson-Williams",
            occupation: "Senior Manager",
            email: updatedEmail,
            mobile: "+447123999888");

        // Act
        var updateResponse = await Client.PutAsJsonAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getCustomerResponse = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedCustomer = await getCustomerResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedCustomer);
        Assert.Equal(updateRequest.PersonalInfo.LastName, updatedCustomer.PersonalInfo.LastName);
        Assert.Equal(updateRequest.ContactInfo.Email, updatedCustomer.ContactInfo.Email);

        var getBookingResponse = await Client.GetAsync(new Uri($"/bookings/{booking.Id}", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedBooking = await getBookingResponse.Content.ReadFromJsonAsync<GetBookingDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(updatedBooking);
        Assert.Equal(customer.Id, updatedBooking.CustomerId);
    }
}
