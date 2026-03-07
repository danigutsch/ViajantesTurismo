using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class GetCustomerByIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Customer_By_Id()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Jane", "Smith", TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customerDto = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customerDto);
        Assert.Equal(customer.Id, customerDto.Id);
        Assert.Equal(customer.FirstName, customerDto.PersonalInfo.FirstName);
        Assert.Equal(customer.LastName, customerDto.PersonalInfo.LastName);
    }

    [Fact]
    public async Task Get_Customer_By_Id_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        const int invalidId = -1;

        // Act
        var response = await Client.GetAsync(new Uri($"/customers/{invalidId}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Can_Get_Customer_With_Bookings()
    {
        // Arrange
        var customer = await Client.CreateTestCustomer("Customer", "WithBookings", TestContext.Current.CancellationToken);
        var tour1 = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var tour2 = await Client.CreateTestTour("CUBA2025", "Cuba Adventure 2025", cancellationToken: TestContext.Current.CancellationToken);
        await Client.CreateTestBooking(tour1.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.CreateTestBooking(tour2.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(new Uri($"/customers/{customer.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customerDto = await response.Content.ReadFromJsonAsync<CustomerDetailsDto>(cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(customerDto);
        Assert.Equal(customer.Id, customerDto.Id);
        var bookingsResponse = await Client.GetBookingsByCustomer(customer.Id, TestContext.Current.CancellationToken);
        var bookings = await bookingsResponse.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
    }
}
