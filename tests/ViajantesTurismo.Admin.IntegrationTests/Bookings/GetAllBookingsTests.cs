using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class GetAllBookingsTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Get_Multiple_Bookings()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer1 = await Client.CreateTestCustomer("Alice", "Johnson", cancellationToken: TestContext.Current.CancellationToken);
        var customer2 = await Client.CreateTestCustomer("Charlie", "Brown", cancellationToken: TestContext.Current.CancellationToken);

        var booking1 = await Client.CreateTestBooking(tourDto.Id, customer1.Id, cancellationToken: TestContext.Current.CancellationToken);
        var booking2 = await Client.CreateTestBooking(tourDto.Id, customer2.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);

        var createdIds = new HashSet<Guid>
        {
            booking1.Id,
            booking2.Id,
        };

        var createdBookings = bookings.Where(b => createdIds.Contains(b.Id)).ToArray();

        Assert.Equal(2, createdBookings.Length);
        Assert.Contains(createdBookings, b => b.Id == booking1.Id);
        Assert.Contains(createdBookings, b => b.Id == booking2.Id);
    }

    [Fact]
    public async Task GetAllBookings_Returns_Correct_Payment_Status_After_Payments()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Payment", "ListCheck", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var payment = DtoBuilders.BuildCreatePaymentDto(amount: 500m);
        var paymentResponse = await Client.RecordPayment(booking.Id, payment, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);

        var singleBooking = await Client.GetBookingAndRead(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, singleBooking.PaymentStatus);
        Assert.Equal(500m, singleBooking.AmountPaid);

        // Act
        var allBookings = await Client.GetAllBookingsAndReadAsync(TestContext.Current.CancellationToken);

        // Assert
        var listBooking = Assert.Single(allBookings, b => b.Id == booking.Id);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, listBooking.PaymentStatus);
        Assert.Equal(500m, listBooking.AmountPaid);
        Assert.True(listBooking.RemainingBalance > 0);
        Assert.Single(listBooking.Payments);
    }
}

public sealed class GetAllBookingsEmptyListTests(ApiFixture fixture) : AdminApiSerialTestBase(fixture)
{
    [Fact]
    [Trait("SeedDependency", "Intentional-EmptyState-Smoke")]
    public async Task Can_Get_Empty_Booking_List()
    {
        // Arrange
        await ClearDatabaseAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAllBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bookings = await response.Content.ReadFromJsonAsync<GetBookingDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }
}
