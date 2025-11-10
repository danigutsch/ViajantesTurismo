using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class PaymentTrackingSteps(TourContext tourContext, BookingContext bookingContext)
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private Result<Payment> _paymentResult;
    private Payment? _retrievedPayment;

    [Then("the booking should have (.*) payments")]
    public void ThenTheBookingShouldHavePayments(int expectedCount)
    {
        Assert.Equal(expectedCount, bookingContext.Booking.Payments.Count);
    }

    [Then("the payment history should be empty")]
    public void ThenThePaymentHistoryShouldBeEmpty()
    {
        Assert.Empty(bookingContext.Booking.Payments);
    }

    [Then("the first payment amount should be (.*)")]
    public void ThenTheFirstPaymentAmountShouldBe(decimal expectedAmount)
    {
        var firstPayment = bookingContext.Booking.Payments.First();
        Assert.Equal(expectedAmount, firstPayment.Amount);
    }

    [Then("the second payment amount should be (.*)")]
    public void ThenTheSecondPaymentAmountShouldBe(decimal expectedAmount)
    {
        var secondPayment = bookingContext.Booking.Payments.Skip(1).First();
        Assert.Equal(expectedAmount, secondPayment.Amount);
    }

    [Then("the payment history should be ordered by payment date")]
    public void ThenThePaymentHistoryShouldBeOrderedByPaymentDate()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var orderedPayments = payments.OrderBy(p => p.PaymentDate).ToList();

        for (var i = 0; i < payments.Count; i++)
        {
            Assert.Equal(orderedPayments[i].Id, payments[i].Id);
        }
    }

    [Then("the payment should have a recorded timestamp")]
    public void ThenThePaymentShouldHaveARecordedTimestamp()
    {
        var payment = bookingContext.Booking.Payments.Last();
        Assert.NotEqual(default, payment.RecordedAt);
    }

    [Then("the recorded timestamp should be recent")]
    public void ThenTheRecordedTimestampShouldBeRecent()
    {
        var payment = bookingContext.Booking.Payments.Last();
        var timeDifference = DateTime.UtcNow - payment.RecordedAt;
        Assert.True(timeDifference.TotalMinutes < 1,
            $"Timestamp should be recent, but was {timeDifference.TotalMinutes} minutes ago");
    }

    [When("I retrieve the payment by its ID")]
    public void WhenIRetrieveThePaymentByItsId()
    {
        var payment = bookingContext.Booking.Payments.Last();
        _retrievedPayment = bookingContext.Booking.Payments.FirstOrDefault(p => p.Id == payment.Id);
        Assert.NotNull(_retrievedPayment);
    }

    [Then("the payment details should match the recorded payment")]
    public void ThenThePaymentDetailsShouldMatchTheRecordedPayment()
    {
        var originalPayment = bookingContext.Booking.Payments.Last();
        Assert.NotNull(_retrievedPayment);
        Assert.Equal(originalPayment.Id, _retrievedPayment.Id);
        Assert.Equal(originalPayment.Amount, _retrievedPayment.Amount);
        Assert.Equal(originalPayment.PaymentDate, _retrievedPayment.PaymentDate);
        Assert.Equal(originalPayment.Method, _retrievedPayment.Method);
    }

    [Then("each payment should have its distinct method")]
    public void ThenEachPaymentShouldHaveItsDistinctMethod()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var distinctMethods = payments.Select(p => p.Method).Distinct().Count();
        Assert.Equal(payments.Count, distinctMethods);
    }

    [Then("the payment amount should be sanitized to valid precision")]
    public void ThenThePaymentAmountShouldBeSanitizedToValidPrecision()
    {
        var payment = bookingContext.Booking.Payments.Last();
        var amountString = payment.Amount.ToString("F2", CultureInfo.InvariantCulture);
        var roundedAmount = decimal.Parse(amountString, CultureInfo.InvariantCulture);
        Assert.Equal(roundedAmount, payment.Amount);
    }

    [Then("the payment notes should be sanitized")]
    public void ThenThePaymentNotesShouldBeSanitized()
    {
        var payment = bookingContext.Booking.Payments.Last();
        Assert.NotNull(payment.Notes);
        Assert.DoesNotContain("😊", payment.Notes, StringComparison.Ordinal);
        Assert.DoesNotContain("👍", payment.Notes, StringComparison.Ordinal);
    }

    [When("I record a payment with today's date")]
    public void WhenIRecordAPaymentWithTodaysDate()
    {
        var today = DateTime.UtcNow.Date;
        _paymentResult = bookingContext.Booking.RecordPayment(100m, today, PaymentMethod.Cash, _timeProvider);
        bookingContext.Result = _paymentResult;
    }

    [When("I attempt to record a payment with tomorrow's date")]
    public void WhenIAttemptToRecordAPaymentWithTomorrowsDate()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, tomorrow, PaymentMethod.Cash, _timeProvider);
        bookingContext.Result = _paymentResult;
    }

    [When("I attempt to record a payment with a negative payment method value")]
    public void WhenIAttemptToRecordAPaymentWithANegativePaymentMethodValue()
    {
        var paymentDate = DateTime.UtcNow.AddDays(-1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, paymentDate, (PaymentMethod)(-1), _timeProvider);
        bookingContext.Result = _paymentResult;
    }

    [Given("the booking is cancelled")]
    public void GivenTheBookingIsCancelled()
    {
        tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
    }

    [Given("the booking is completed")]
    public void GivenTheBookingIsCompleted()
    {
        tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
    }

    [Given("the booking is confirmed")]
    public void GivenTheBookingIsConfirmed()
    {
        tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
    }

    [Given("the booking is pending")]
    public void GivenTheBookingIsPending()
    {
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Then("the payments should maintain their recording order")]
    public void ThenThePaymentsShouldMaintainTheirRecordingOrder()
    {
        var payments = bookingContext.Booking.Payments.ToList();

        Assert.Equal(100m, payments[0].Amount);
        Assert.Equal(new DateTime(2025, 3, 15), payments[0].PaymentDate);

        Assert.Equal(200m, payments[1].Amount);
        Assert.Equal(new DateTime(2025, 1, 15), payments[1].PaymentDate);

        Assert.Equal(150m, payments[2].Amount);
        Assert.Equal(new DateTime(2025, 2, 15), payments[2].PaymentDate);
    }

    [Then("all payments should have the same payment date")]
    public void ThenAllPaymentsShouldHaveTheSamePaymentDate()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var firstDate = payments[0].PaymentDate;
        Assert.All(payments, p => Assert.Equal(firstDate, p.PaymentDate));
    }

    [Given(@"I record a payment of (.*) on (.*) using (.*) with reference ""(.*)""")]
    public void GivenIRecordAPaymentOfOnUsingWithReference(decimal amount, string dateString, string methodString,
        string reference)
    {
        var paymentDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var method = Enum.Parse<PaymentMethod>(methodString);

        var result = bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider, reference);
        Assert.True(result.IsSuccess);
    }

    [Given("another tour exists with a pending booking for payment tests")]
    public void GivenAnotherTourExistsWithAPendingBookingForPaymentTests()
    {
        tourContext.Tour = TestHelpers.CreateTestTourForPaymentTests();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I record a payment for the second booking with reference ""(.*)""")]
    public void WhenIRecordAPaymentForTheSecondBookingWithReference(string reference)
    {
        var result = bookingContext.Booking.RecordPayment(100m, DateTime.UtcNow.AddDays(-1), PaymentMethod.CreditCard,
            _timeProvider, reference);
        Assert.True(result.IsSuccess);
    }

    [Then("both payments should have the same reference number")]
    public void ThenBothPaymentsShouldHaveTheSameReferenceNumber()
    {
        var payment = bookingContext.Booking.Payments.Last();
        Assert.Equal("REF-123", payment.ReferenceNumber);
    }

    [Given("the booking has a (.*)% discount applied")]
    public void GivenTheBookingHasADiscountApplied(decimal discountPercentage)
    {
        tourContext.Tour = TestHelpers.CreateTestTourForPaymentTests();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.Percentage, discountPercentage, "Test discount", null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given("the booking total price is (.*)")]
    public void GivenTheBookingTotalPriceIs(decimal expectedTotal)
    {
        Assert.Equal(expectedTotal, bookingContext.Booking.TotalPrice);
    }
}
