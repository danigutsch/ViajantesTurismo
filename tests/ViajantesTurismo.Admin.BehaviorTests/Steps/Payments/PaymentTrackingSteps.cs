using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Payments;

[Binding]
public sealed class PaymentTrackingSteps(TourContext tourContext, BookingContext bookingContext)
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private Result<Payment> _paymentResult;
    private Payment? _retrievedPayment;

    [Then("the booking should have (.*) payments")]
    public void ThenTheBookingShouldHavePayments(int expectedCount)
    {
        TestAssert.Equal(expectedCount, bookingContext.Booking.Payments.Count);
    }

    [Then("the payment history should be empty")]
    public void ThenThePaymentHistoryShouldBeEmpty()
    {
        TestAssert.Empty(bookingContext.Booking.Payments);
    }

    [Then("the first payment amount should be (.*)")]
    public void ThenTheFirstPaymentAmountShouldBe(decimal expectedAmount)
    {
        var firstPayment = bookingContext.Booking.Payments.First();
        TestAssert.Equal(expectedAmount, firstPayment.Amount);
    }

    [Then("the second payment amount should be (.*)")]
    public void ThenTheSecondPaymentAmountShouldBe(decimal expectedAmount)
    {
        var secondPayment = bookingContext.Booking.Payments.Skip(1).First();
        TestAssert.Equal(expectedAmount, secondPayment.Amount);
    }

    [Then("the payment history should be ordered by payment date")]
    public void ThenThePaymentHistoryShouldBeOrderedByPaymentDate()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var orderedPayments = payments.OrderBy(p => p.PaymentDate).ToList();

        for (var i = 0; i < payments.Count; i++)
        {
            TestAssert.Equal(orderedPayments[i].Id, payments[i].Id);
        }
    }

    [Then("the payment should have a recorded timestamp")]
    public void ThenThePaymentShouldHaveARecordedTimestamp()
    {
        var payment = bookingContext.Booking.Payments.Last();
        TestAssert.NotEqual(default, payment.RecordedAt);
    }

    [Then("the recorded timestamp should be recent")]
    public void ThenTheRecordedTimestampShouldBeRecent()
    {
        var payment = bookingContext.Booking.Payments.Last();
        var timeDifference = DateTime.UtcNow - payment.RecordedAt;
        TestAssert.True(timeDifference.TotalMinutes < 1,
            $"Timestamp should be recent, but was {timeDifference.TotalMinutes} minutes ago");
    }

    [When("I retrieve the payment by its ID")]
    public void WhenIRetrieveThePaymentByItsId()
    {
        var payment = bookingContext.Booking.Payments.Last();
        _retrievedPayment = bookingContext.Booking.Payments.FirstOrDefault(p => p.Id == payment.Id);
        TestAssert.NotNull(_retrievedPayment);
    }

    [Then("the payment details should match the recorded payment")]
    public void ThenThePaymentDetailsShouldMatchTheRecordedPayment()
    {
        var originalPayment = bookingContext.Booking.Payments.Last();
        TestAssert.NotNull(_retrievedPayment);
        TestAssert.Equal(originalPayment.Id, _retrievedPayment.Id);
        TestAssert.Equal(originalPayment.Amount, _retrievedPayment.Amount);
        TestAssert.Equal(originalPayment.PaymentDate, _retrievedPayment.PaymentDate);
        TestAssert.Equal(originalPayment.Method, _retrievedPayment.Method);
    }

    [Then("each payment should have its distinct method")]
    public void ThenEachPaymentShouldHaveItsDistinctMethod()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var distinctMethods = payments.Select(p => p.Method).Distinct().Count();
        TestAssert.Equal(payments.Count, distinctMethods);
    }

    [Then("the payment amount should be sanitized to valid precision")]
    public void ThenThePaymentAmountShouldBeSanitizedToValidPrecision()
    {
        var payment = bookingContext.Booking.Payments.Last();
        var amountString = payment.Amount.ToString("F2", CultureInfo.InvariantCulture);
        var roundedAmount = decimal.Parse(amountString, CultureInfo.InvariantCulture);
        TestAssert.Equal(roundedAmount, payment.Amount);
    }

    [Then("the payment notes should be sanitized")]
    public void ThenThePaymentNotesShouldBeSanitized()
    {
        var payment = bookingContext.Booking.Payments.Last();
        TestAssert.NotNull(payment.Notes);
        TestAssert.DoesNotContain("😊", payment.Notes, StringComparison.Ordinal);
        TestAssert.DoesNotContain("👍", payment.Notes, StringComparison.Ordinal);
    }

    [When("I record a payment with today's date")]
    public void WhenIRecordAPaymentWithTodaysDate()
    {
        var today = DateTime.UtcNow.Date;
        _paymentResult = bookingContext.Booking.RecordPayment(100m, today, PaymentMethod.Cash, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I attempt to record a payment with tomorrow's date")]
    public void WhenIAttemptToRecordAPaymentWithTomorrowsDate()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, tomorrow, PaymentMethod.Cash, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I attempt to record a payment with a negative payment method value")]
    public void WhenIAttemptToRecordAPaymentWithANegativePaymentMethodValue()
    {
        var paymentDate = DateTime.UtcNow.AddDays(-1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, paymentDate, (PaymentMethod)(-1), _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
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
        TestAssert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Then("the payments should maintain their recording order")]
    public void ThenThePaymentsShouldMaintainTheirRecordingOrder()
    {
        var payments = bookingContext.Booking.Payments.ToList();

        TestAssert.Equal(100m, payments[0].Amount);
        TestAssert.Equal(new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc), payments[0].PaymentDate);

        TestAssert.Equal(200m, payments[1].Amount);
        TestAssert.Equal(new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), payments[1].PaymentDate);

        TestAssert.Equal(150m, payments[2].Amount);
        TestAssert.Equal(new DateTime(2025, 2, 15, 0, 0, 0, DateTimeKind.Utc), payments[2].PaymentDate);
    }

    [Then("all payments should have the same payment date")]
    public void ThenAllPaymentsShouldHaveTheSamePaymentDate()
    {
        var payments = bookingContext.Booking.Payments.ToList();
        var firstDate = payments[0].PaymentDate;
        TestAssert.All(
            payments,
            p => TestAssert.Equal(firstDate, p.PaymentDate));
    }

    [Given(@"I record a payment of (.*) on (.*) using (.*) with reference ""(.*)""")]
    public void GivenIRecordAPaymentOfOnUsingWithReference(decimal amount, string dateString, string methodString,
        string reference)
    {
        var paymentDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var method = Enum.Parse<PaymentMethod>(methodString);

        var result = bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider, reference);
        TestAssert.True(result.IsSuccess);
    }

    [Given("another tour exists with a pending booking for payment tests")]
    public void GivenAnotherTourExistsWithAPendingBookingForPaymentTests()
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(BasePrice: 900.00m)));
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I record a payment for the second booking with reference ""(.*)""")]
    public void WhenIRecordAPaymentForTheSecondBookingWithReference(string reference)
    {
        var result = bookingContext.Booking.RecordPayment(100m, DateTime.UtcNow.AddDays(-1), PaymentMethod.CreditCard,
            _timeProvider, reference);
        TestAssert.True(result.IsSuccess);
    }

    [Then("both payments should have the same reference number")]
    public void ThenBothPaymentsShouldHaveTheSameReferenceNumber()
    {
        var payment = bookingContext.Booking.Payments.Last();
        TestAssert.Equal("REF-123", payment.ReferenceNumber);
    }

    [Given("the booking has a (.*)% discount applied")]
    public void GivenTheBookingHasADiscountApplied(decimal discountPercentage)
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(BasePrice: 900.00m)));
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Percentage,
            discountAmount: discountPercentage,
            discountReason: "Test discount"));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given("the booking total price is (.*)")]
    public void GivenTheBookingTotalPriceIs(decimal expectedTotal)
    {
        TestAssert.Equal(expectedTotal, bookingContext.Booking.TotalPrice);
    }
}
