using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetBookingsByCustomerIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Bookings_By_Customer_Id()
    {
        // Arrange
        var tour1 = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var tour2 = await Client.CreateTestTour("CUBA2025", "Cuba Adventure 2025", cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Grace", "Lee", cancellationToken: TestContext.Current.CancellationToken);

        await Client.CreateTestBooking(tour1.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.CreateTestBooking(tour2.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBookingsByCustomer(customerDto.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(customerDto.Id, b.CustomerId));
    }

    [Fact]
    public async Task Get_Bookings_By_Customer_Id_Includes_Bookings_As_Companion()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var primaryCustomer = await Client.CreateTestCustomer("Henry", "Taylor", cancellationToken: TestContext.Current.CancellationToken);
        var companionCustomer = await Client.CreateTestCustomer("Iris", "Anderson", cancellationToken: TestContext.Current.CancellationToken);

        await Client.CreateTestBooking(tourDto.Id, primaryCustomer.Id, companionCustomer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBookingsByCustomer(companionCustomer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Single(bookings);
        Assert.Equal(companionCustomer.Id, bookings[0].CompanionId);
    }

    [Fact]
    public async Task Get_Bookings_By_Customer_Id_Returns_Empty_For_Invalid_Customer()
    {
        // Act
        var nonExistingCustomerId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetBookingsByCustomer(nonExistingCustomerId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task Can_Get_Bookings_Where_Customer_Is_Both_Principal_And_Companion()
    {
        // Arrange
        var tour1 = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var tour2 = await Client.CreateTestTour("CUBA2025", "Cuba Adventure 2025", cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Multi", "Role", cancellationToken: TestContext.Current.CancellationToken);
        var otherCustomer = await Client.CreateTestCustomer("Other", "Person", cancellationToken: TestContext.Current.CancellationToken);
        var booking1 = await Client.CreateTestBooking(tour1.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var booking2 = await Client.CreateTestBooking(tour2.Id, otherCustomer.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBookingsByCustomer(customer.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.Contains(bookings, b => b.Id == booking1.Id && b.CustomerId == customer.Id);
        Assert.Contains(bookings, b => b.Id == booking2.Id && b.CompanionId == customer.Id);
    }
}
