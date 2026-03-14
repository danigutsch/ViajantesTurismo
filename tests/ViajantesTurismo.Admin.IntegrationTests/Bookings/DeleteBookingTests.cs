namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class DeleteBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Delete_Pending_Booking()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Kate", "White", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await Client.DeleteBooking(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await Client.GetBooking(createdBooking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.DeleteBooking(nonExistingId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Delete_Confirmed_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Delete", "Confirmed", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Confirmed, confirmed!.Status);

        // Act
        var deleteResponse = await Client.DeleteBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var getResponse = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Cannot_Delete_Completed_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Delete", "Completed", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var completed = await completeResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Completed, completed!.Status);

        // Act
        var deleteResponse = await Client.DeleteBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var getResponse = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Can_Delete_Cancelled_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Delete", "Cancelled", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Cancelled, cancelled!.Status);

        // Act
        var deleteResponse = await Client.DeleteBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var getResponse = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
