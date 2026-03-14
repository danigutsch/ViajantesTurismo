using System.Net;
using System.Net.Http.Json;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Builders;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Bookings;

public sealed class PaymentWorkflowTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Payment_Workflow_Multiple_Partial_Payments_To_Full_Payment()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Partial", "Payments", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatusDto.Unpaid, booking.PaymentStatus);
        Assert.Equal(0m, booking.AmountPaid);
        Assert.Equal(booking.TotalPrice, booking.RemainingBalance);

        var payment1 = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice * 0.25m,
            method: PaymentMethodDto.CreditCard,
            referenceNumber: "PAY-001");
        var response1 = await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        var booking1 = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var updated1 = await booking1.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated1);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, updated1.PaymentStatus);
        Assert.Equal(booking.TotalPrice * 0.25m, updated1.AmountPaid);
        Assert.Equal(booking.TotalPrice * 0.75m, updated1.RemainingBalance);

        var payment2 = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice * 0.50m,
            method: PaymentMethodDto.BankTransfer,
            referenceNumber: "PAY-002");
        var response2 = await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var booking2 = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var updated2 = await booking2.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(updated2);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, updated2.PaymentStatus);
        Assert.Equal(booking.TotalPrice * 0.75m, updated2.AmountPaid);
        Assert.Equal(booking.TotalPrice * 0.25m, updated2.RemainingBalance);

        var payment3 = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice * 0.25m,
            method: PaymentMethodDto.Cash,
            referenceNumber: "PAY-003");
        var response3 = await Client.RecordPayment(booking.Id, payment3, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response3.StatusCode);
        var booking3 = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var finalBooking = await booking3.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(finalBooking);
        Assert.Equal(PaymentStatusDto.Paid, finalBooking.PaymentStatus);
        Assert.Equal(booking.TotalPrice, finalBooking.AmountPaid);
        Assert.Equal(0m, finalBooking.RemainingBalance);
        Assert.Equal(3, finalBooking.Payments.Count);
    }

    [Fact]
    public async Task Payment_Workflow_Single_Full_Payment()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Full", "Payment", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var payment = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice,
            method: PaymentMethodDto.CreditCard,
            referenceNumber: "FULL-PAY");
        var response = await Client.RecordPayment(booking.Id, payment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(PaymentStatusDto.Paid, result.PaymentStatus);
        Assert.Equal(booking.TotalPrice, result.AmountPaid);
        Assert.Equal(0m, result.RemainingBalance);
        Assert.Single(result.Payments);
    }

    [Fact]
    public async Task Cannot_Record_Payment_Exceeding_Remaining_Balance()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Overpay", "Test", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var firstPayment = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice * 0.50m,
            method: PaymentMethodDto.Cash);
        await Client.RecordPayment(booking.Id, firstPayment, TestContext.Current.CancellationToken);

        // Act
        var excessPayment = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice * 0.60m,
            method: PaymentMethodDto.CreditCard);
        var response = await Client.RecordPayment(booking.Id, excessPayment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cannot_Record_Payment_When_Already_Fully_Paid()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Already", "Paid", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var fullPayment = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice,
            method: PaymentMethodDto.BankTransfer);
        await Client.RecordPayment(booking.Id, fullPayment, TestContext.Current.CancellationToken);

        // Act
        var extraPayment = DtoBuilders.BuildCreatePaymentDto(
            amount: 1m,
            method: PaymentMethodDto.Cash);
        var response = await Client.RecordPayment(booking.Id, extraPayment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Payment_Workflow_With_Different_Payment_Methods()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Mixed", "Methods", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var amountPerPayment = booking.TotalPrice / 4m;

        // Act
        var payment1 = DtoBuilders.BuildCreatePaymentDto(amount: amountPerPayment, method: PaymentMethodDto.CreditCard, referenceNumber: "CC-001");
        var payment2 = DtoBuilders.BuildCreatePaymentDto(amount: amountPerPayment, method: PaymentMethodDto.BankTransfer, referenceNumber: "BT-002");
        var payment3 = DtoBuilders.BuildCreatePaymentDto(amount: amountPerPayment, method: PaymentMethodDto.Cash, referenceNumber: "CASH-003");
        var payment4 = DtoBuilders.BuildCreatePaymentDto(amount: amountPerPayment, method: PaymentMethodDto.Check, referenceNumber: "CHECK-004");

        await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);
        await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);
        await Client.RecordPayment(booking.Id, payment3, TestContext.Current.CancellationToken);
        var finalResponse = await Client.RecordPayment(booking.Id, payment4, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, finalResponse.StatusCode);
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(PaymentStatusDto.Paid, result.PaymentStatus);
        Assert.Equal(4, result.Payments.Count);
        Assert.Contains(result.Payments, p => p.Method == PaymentMethodDto.CreditCard);
        Assert.Contains(result.Payments, p => p.Method == PaymentMethodDto.BankTransfer);
        Assert.Contains(result.Payments, p => p.Method == PaymentMethodDto.Cash);
        Assert.Contains(result.Payments, p => p.Method == PaymentMethodDto.Check);
    }

    [Fact]
    public async Task Payment_Workflow_Tracks_Payment_History()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Payment", "History", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var payment1Amount = 300m;
        var payment2Amount = 450m;
        var payment3Amount = booking.TotalPrice - payment1Amount - payment2Amount;

        // Act
        var payment1 = DtoBuilders.BuildCreatePaymentDto(amount: payment1Amount, method: PaymentMethodDto.Cash, referenceNumber: "HIST-001", notes: "Initial deposit");
        var payment2 = DtoBuilders.BuildCreatePaymentDto(amount: payment2Amount, method: PaymentMethodDto.CreditCard, referenceNumber: "HIST-002", notes: "Second installment");
        var payment3 = DtoBuilders.BuildCreatePaymentDto(amount: payment3Amount, method: PaymentMethodDto.BankTransfer, referenceNumber: "HIST-003", notes: "Final payment");

        await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);
        await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);
        await Client.RecordPayment(booking.Id, payment3, TestContext.Current.CancellationToken);

        // Assert
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(3, result.Payments.Count);
        Assert.Equal(payment1Amount, result.Payments.ElementAt(0).Amount);
        Assert.Equal(payment2Amount, result.Payments.ElementAt(1).Amount);
        Assert.Equal(payment3Amount, result.Payments.ElementAt(2).Amount);
        Assert.Equal("Initial deposit", result.Payments.ElementAt(0).Notes);
        Assert.Equal("Second installment", result.Payments.ElementAt(1).Notes);
        Assert.Equal("Final payment", result.Payments.ElementAt(2).Notes);
    }

    [Fact]
    public async Task Payment_Workflow_Status_Progression_Unpaid_To_Partial_To_Paid()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Status", "Progression", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PaymentStatusDto.Unpaid, booking.PaymentStatus);

        var payment1 = DtoBuilders.BuildCreatePaymentDto(amount: 100m, method: PaymentMethodDto.Cash);
        await Client.RecordPayment(booking.Id, payment1, TestContext.Current.CancellationToken);

        var booking1 = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var status1 = await booking1.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(status1);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, status1.PaymentStatus);

        var payment2 = DtoBuilders.BuildCreatePaymentDto(amount: 200m, method: PaymentMethodDto.CreditCard);
        await Client.RecordPayment(booking.Id, payment2, TestContext.Current.CancellationToken);

        var booking2 = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var status2 = await booking2.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(status2);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, status2.PaymentStatus);

        var remainingAmount = booking.TotalPrice - 300m;
        var payment3 = DtoBuilders.BuildCreatePaymentDto(amount: remainingAmount, method: PaymentMethodDto.BankTransfer);
        await Client.RecordPayment(booking.Id, payment3, TestContext.Current.CancellationToken);

        // Assert
        var finalBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var finalStatus = await finalBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(finalStatus);
        Assert.Equal(PaymentStatusDto.Paid, finalStatus.PaymentStatus);
    }

    [Fact]
    public async Task Payment_Workflow_With_Booking_Discount()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Discount", "Payment", TestContext.Current.CancellationToken);

        var bookingRequest = DtoBuilders.BuildCreateBookingDto(
            tourId: tour.Id,
            principalCustomerId: customer.Id,
            discountType: DiscountTypeDto.Percentage,
            discountAmount: 10m);
        var createResponse = await Client.CreateBooking(bookingRequest, TestContext.Current.CancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var booking = await createResponse.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(booking);

        var discountedTotal = booking.TotalPrice;

        // Act
        var payment = DtoBuilders.BuildCreatePaymentDto(
            amount: discountedTotal,
            method: PaymentMethodDto.CreditCard);
        var response = await Client.RecordPayment(booking.Id, payment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(PaymentStatusDto.Paid, result.PaymentStatus);
        Assert.Equal(discountedTotal, result.AmountPaid);
        Assert.Equal(0m, result.RemainingBalance);
    }

    [Fact]
    public async Task Payment_Workflow_Payment_Then_Status_Transition()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Payment", "ThenConfirm", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        var payment = DtoBuilders.BuildCreatePaymentDto(
            amount: booking.TotalPrice,
            method: PaymentMethodDto.CreditCard);
        await Client.RecordPayment(booking.Id, payment, TestContext.Current.CancellationToken);

        var confirmResponse = await Client.ConfirmBooking(booking.Id, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        // Assert
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(BookingStatusDto.Confirmed, result.Status);
        Assert.Equal(PaymentStatusDto.Paid, result.PaymentStatus);
    }

    [Fact]
    public async Task Payment_Workflow_Can_Accept_Payments_For_Cancelled_Booking()
    {
        // Arrange
        var tour = await Client.CreateTestTour(cancellationToken: TestContext.Current.CancellationToken);
        var customer = await Client.CreateTestCustomer("Cancelled", "Payment", TestContext.Current.CancellationToken);
        var booking = await Client.CreateTestBooking(tour.Id, customer.Id, cancellationToken: TestContext.Current.CancellationToken);

        await Client.CancelBooking(booking.Id, TestContext.Current.CancellationToken);

        // Act
        var payment = DtoBuilders.BuildCreatePaymentDto(amount: 100m, method: PaymentMethodDto.Cash);
        var response = await Client.RecordPayment(booking.Id, payment, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var updatedBooking = await Client.GetBooking(booking.Id, TestContext.Current.CancellationToken);
        var result = await updatedBooking.Content.ReadFromJsonAsync<GetBookingDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(BookingStatusDto.Cancelled, result.Status);
        Assert.Equal(PaymentStatusDto.PartiallyPaid, result.PaymentStatus);
    }
}
