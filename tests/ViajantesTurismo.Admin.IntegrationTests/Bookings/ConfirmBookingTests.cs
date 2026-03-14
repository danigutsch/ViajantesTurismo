namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class ConfirmBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Confirm_Pending_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("John", "Confirm", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Pending, booking.Status);

        // Act
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(confirmed);
        Assert.Equal(BookingStatusDto.Confirmed, confirmed.Status);
        Assert.Equal(booking.Id, confirmed.Id);
    }

    [Fact]
    public async Task Confirm_Booking_Returns_NotFound_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.ConfirmBooking(nonExistingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Cancelled_Booking_Returns_Conflict()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Cancel", "Then Confirm", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        // Act
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, confirmResponse.StatusCode);
    }

    [Fact]
    public async Task Confirm_Already_Confirmed_Booking_Is_Idempotent()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Double", "Confirm", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var firstConfirm = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstConfirm.StatusCode);

        // Act
        var secondConfirm = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondConfirm.StatusCode);
        var confirmed = await secondConfirm.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(confirmed);
        Assert.Equal(BookingStatusDto.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task Confirm_Completed_Booking_Returns_Conflict()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Complete", "Then Confirm", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Act
        var secondConfirm = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondConfirm.StatusCode);
    }
}
