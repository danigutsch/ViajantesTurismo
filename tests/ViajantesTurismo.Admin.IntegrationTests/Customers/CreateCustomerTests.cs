using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class CreateCustomerTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Customer()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(
            firstName: "John",
            lastName: "Doe");

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<GetCustomerDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(customer);
        Assert.Equal(request.PersonalInfo.FirstName, customer.FirstName);
        Assert.Equal(request.PersonalInfo.LastName, customer.LastName);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Empty_FirstName()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "John", lastName: "Doe");
        request = request with
        {
            PersonalInfo = request.PersonalInfo with { FirstName = "" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Empty_LastName()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "John", lastName: "Doe");
        request = request with
        {
            PersonalInfo = request.PersonalInfo with { LastName = "" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Empty_Email()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "John", lastName: "Doe");
        request = request with
        {
            ContactInfo = request.ContactInfo with { Email = "" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Future_BirthDate()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto(firstName: "John", lastName: "Doe");
        request = request with
        {
            PersonalInfo = request.PersonalInfo with { BirthDate = DateTime.UtcNow.AddDays(1) }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Duplicate_Email()
    {
        // Arrange
        var duplicateEmail = TestDataGenerator.UniqueEmail("duplicate");
        var firstRequest = DtoBuilders.BuildCreateCustomerDto("First", "Customer");
        firstRequest = firstRequest with
        {
            ContactInfo = firstRequest.ContactInfo with { Email = duplicateEmail }
        };
        var secondRequest = DtoBuilders.BuildCreateCustomerDto("Second", "Customer");
        secondRequest = secondRequest with
        {
            ContactInfo = secondRequest.ContactInfo with { Email = duplicateEmail }
        };

        await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), firstRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), secondRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Invalid_Email_Format()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto("Jane", "Doe");
        request = request with
        {
            ContactInfo = request.ContactInfo with { Email = "not-an-email" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Invalid_Phone_Format()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto("Jane", "Doe");
        request = request with
        {
            ContactInfo = request.ContactInfo with { Mobile = "abc" }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Create_Customer_With_Age_Less_Than_Minimum()
    {
        // Arrange
        var request = DtoBuilders.BuildCreateCustomerDto("Young", "Child");
        request = request with
        {
            PersonalInfo = request.PersonalInfo with { BirthDate = DateTime.UtcNow.AddYears(-9) }
        };

        // Act
        var response = await Client.PostAsJsonAsync(new Uri("/customers", UriKind.Relative), request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
