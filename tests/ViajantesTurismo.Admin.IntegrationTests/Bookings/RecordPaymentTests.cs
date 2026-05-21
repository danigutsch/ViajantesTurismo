namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class RecordPaymentTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal PaymentAmountExceedingRemainingBalance = 3000m;

    [Fact]
    public async Task Record_Payment_Exceeds_RemainingBalance_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Bad", "Pay", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            amount: PaymentAmountExceedingRemainingBalance,
            method: PaymentMethodDto.Cash);

        // Act
        var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_Returns_NotFound_For_Invalid_Booking_Id()
    {
        // Arrange
        var nonExistingBookingId = Guid.CreateVersion7();
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            method: PaymentMethodDto.Cash,
            referenceNumber: "REF-123",
            notes: "Test payment");

        // Act
        var response = await Client.RecordPayment(nonExistingBookingId, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_All_Methods_Updates_Status()
    {
        var paymentMethods = new[]
        {
            PaymentMethodDto.CreditCard,
            PaymentMethodDto.BankTransfer,
            PaymentMethodDto.Cash,
            PaymentMethodDto.Check,
            PaymentMethodDto.PayPal,
            PaymentMethodDto.Other
        };

        foreach (var method in paymentMethods)
        {
            // Arrange
            var tour = await Client.CreateTestTour($"CUBA2024-{method}", $"Cuba Adventure 2024 - {method}", cancellationToken: TestContext.Current.CancellationToken);
            var customer = await Client.CreateTestCustomer($"Pay{method}", "Test", cancellationToken: TestContext.Current.CancellationToken);
            var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

            var paymentDto = DtoBuilders.BuildCreatePaymentDto(
                method: method,
                referenceNumber: $"REF-{method}",
                notes: $"Payment via {method}");

            // Act
            var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var getBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
            var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
            Assert.Single(updated.Payments);
            Assert.Equal(method, updated.Payments.First().Method);
        }
    }

    [Fact]
    public async Task Record_Payment_With_Future_Date_Returns_ValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Future", "Payment", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var futureDate = DateTime.UtcNow.AddDays(1);
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            amount: 100m,
            paymentDate: futureDate,
            method: PaymentMethodDto.Cash);

        // Act
        var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
