using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class RecordPaymentTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private const decimal FirstPaymentAmount = 1000m;
    private const decimal PaymentAmountExceedingRemainingBalance = 3000m;

    [Fact]
    public async Task Can_Record_Payment_And_Update_Status_To_PartiallyPaid()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Pay", "Part", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            amount: 500m,
            referenceNumber: "REF-500",
            notes: "Down payment");

        // Act
        var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var getBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, updated.PaymentStatus);
        Assert.Single(updated.Payments);
        Assert.Equal(500m, updated.Payments.First().Amount);
    }

    [Fact]
    public async Task Can_Record_Multiple_Payments_And_Update_Status_To_Paid()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Pay", "Full", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var expectedTotal = booking.TotalPrice;
        var secondPaymentAmount = expectedTotal - FirstPaymentAmount;

        var payment1 = DtoBuilders.BuildCreatePaymentDto(
            amount: FirstPaymentAmount,
            method: PaymentMethodDto.CreditCard,
            referenceNumber: "REF-1");
        var response1 = await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var payment2 = DtoBuilders.BuildCreatePaymentDto(
            amount: secondPaymentAmount,
            method: PaymentMethodDto.CreditCard,
            referenceNumber: "REF-2");
        var response2 = await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var getBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        getBooking.EnsureSuccessStatusCode();
        var updated = await getBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated);

        Assert.Equal(PaymentStatusDto.Paid, updated.PaymentStatus);
        Assert.Equal(2, updated.Payments.Count);
        Assert.Equal(expectedTotal, updated.Payments.Sum(p => p.Amount));
    }

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
    public async Task Record_Payment_With_Negative_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Neg", "Payment", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            amount: -100m,
            method: PaymentMethodDto.Cash,
            notes: "Invalid negative");

        // Act
        var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Record_Payment_With_Zero_Amount_ReturnsValidationProblem()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Zero", "Payment", cancellationToken: TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);
        var paymentDto = DtoBuilders.BuildCreatePaymentDto(
            amount: 0m,
            method: PaymentMethodDto.Cash);

        // Act
        var response = await Client.RecordPayment(booking.Id, paymentDto, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
