using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Helpers;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class UpdateCustomerTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Update_Customer()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Alice", "Johnson", TestContext.Current.CancellationToken);

        var updateRequest = DtoBuilders.BuildUpdateCustomerDto(
            firstName: "Alice",
            lastName: "Johnson-Smith",
            profession: "Senior Designer",
            email: "alice.smith@example.com",
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
            roomType: RoomTypeDto.DoubleRoom,
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
        Assert.Equal(updateRequest.PersonalInfo.Profession, updatedCustomer.PersonalInfo.Profession);
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
            profession: "Tester");

        // Act
        var response = await Client.PutAsJsonAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), updateRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
