using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetBookingsByTourIdTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Bookings_By_Tour_Id()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer1 = await Client.CreateTestCustomer("Emma", "Davis", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Frank", "Miller", cancellationToken: TestContext.Current.CancellationToken);

        await Client.CreateTestBooking(tourDto.Id, customer1.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.CreateTestBooking(tourDto.Id, customer2.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBookingsByTour(tourDto.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings =
            await response.Content.ReadFromJsonAsync<GetBookingDto[]>(
                TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(2, bookings.Length);
        Assert.All(bookings, b => Assert.Equal(tourDto.Id, b.TourId));
    }

    [Fact]
    public async Task Get_Bookings_By_Tour_Id_Returns_Empty_For_Invalid_Tour()
    {
        // Act
        var nonExistingTourId = Guid.CreateVersion7();

        // Act
        var response = await Client.GetBookingsByTour(nonExistingTourId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task Can_Get_Multiple_Bookings_With_Different_Statuses()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer1 = await Client.CreateTestCustomer("Multi", "Pending", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Multi", "Confirmed", cancellationToken: TestContext.Current.CancellationToken);
        var customer3 = await Client.CreateTestCustomer("Multi", "Cancelled", cancellationToken: TestContext.Current.CancellationToken);
        var booking1 = await Client.CreateTestBooking(tour.Id, customer1.Id, cancellationToken: TestContext.Current.CancellationToken);
        var booking2 = await Client.CreateTestBooking(tour.Id, customer2.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.ConfirmBooking(booking2.Id, TestContext.Current.CancellationToken);
        var booking3 = await Client.CreateTestBooking(tour.Id, customer3.Id, cancellationToken: TestContext.Current.CancellationToken);
        await Client.CancelBooking(booking3.Id, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetBookingsByTour(tour.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Equal(3, bookings.Length);
        Assert.Contains(bookings, b => b.Id == booking1.Id && b.Status == BookingStatusDto.Pending);
        Assert.Contains(bookings, b => b.Id == booking2.Id && b.Status == BookingStatusDto.Confirmed);
        Assert.Contains(bookings, b => b.Id == booking3.Id && b.Status == BookingStatusDto.Cancelled);
    }
}
