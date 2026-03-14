namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class CancelBookingTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Cancel_Booking()
    {
        // Arrange
        var tourDto = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customerDto = await Client.CreateTestCustomer("Laura", "Brown", cancellationToken: TestContext.Current.CancellationToken);
        var createdBooking = await Client.CreateTestBooking(tourDto.Id, customerDto.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(BookingStatusDto.Pending, createdBooking.Status);

        // Act
        var response = await Client.CancelBooking(createdBooking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelledBooking = await response.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelledBooking);
        Assert.Equal(BookingStatusDto.Cancelled, cancelledBooking.Status);
        Assert.Equal(createdBooking.TotalPrice, cancelledBooking.TotalPrice);
        Assert.Equal(createdBooking.PaymentStatus, cancelledBooking.PaymentStatus);
    }

    [Fact]
    public async Task Cancel_Booking_Returns_Not_Found_For_Invalid_Id()
    {
        // Arrange
        var nonExistingId = Guid.CreateVersion7();

        // Act
        var response = await Client.CancelBooking(nonExistingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_Completed_Booking_Returns_Conflict()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Complete", "Then Cancel", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var completeResponse = await Client.CompleteBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);

        // Act
        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task Cancel_Already_Cancelled_Booking_Is_Idempotent()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Double", "Cancel", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var firstCancel = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstCancel.StatusCode);

        // Act
        var secondCancel = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondCancel.StatusCode);
        var cancelled = await secondCancel.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelled);
        Assert.Equal(BookingStatusDto.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Can_Cancel_Confirmed_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Confirm", "Then Cancel", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal(BookingStatusDto.Confirmed, confirmed!.Status);

        // Act
        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelled);
        Assert.Equal(BookingStatusDto.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Can_Cancel_Booking_With_Payments()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Payment", "Cancel", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var paymentRequest = DtoBuilders.BuildCreatePaymentDto(amount: 100m);
        var paymentResponse = await Client.RecordPayment(booking.Id, paymentRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, paymentResponse.StatusCode);
        var getResponse = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var bookingWithPayment = await getResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.Equal(100m, bookingWithPayment!.AmountPaid);

        // Act
        var cancelResponse = await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(cancelled);
        Assert.Equal(BookingStatusDto.Cancelled, cancelled.Status);
        Assert.Equal(100m, cancelled.AmountPaid);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, cancelled.PaymentStatus);
    }
}
