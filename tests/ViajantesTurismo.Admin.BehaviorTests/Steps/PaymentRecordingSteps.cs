using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class PaymentRecordingSteps(TourContext tourContext, BookingContext bookingContext)
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private Result<Payment> _paymentResult;

    [Given("a tour exists with a pending booking for payment tests")]
    public void GivenATourExistsWithAPendingBookingForPaymentTests()
    {
        tourContext.Tour = EntityBuilders.BuildTour(basePrice: 900.00m);
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.DoubleOccupancy,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [When("I record a payment with the following details:")]
    public void WhenIRecordAPaymentWithTheFollowingDetails(Table table)
    {
        string GetFieldValue(string fieldName)
        {
            var row = table.Rows.FirstOrDefault(r => r["Field"] == fieldName);
            return row?["Value"] ?? string.Empty;
        }

        var amount = decimal.Parse(GetFieldValue("Amount"), CultureInfo.InvariantCulture);
        var paymentDate = DateTime.Parse(GetFieldValue("PaymentDate"), CultureInfo.InvariantCulture);
        var method = Enum.Parse<PaymentMethod>(GetFieldValue("Method"));
        var referenceNumber = string.IsNullOrEmpty(GetFieldValue("ReferenceNumber"))
            ? null
            : GetFieldValue("ReferenceNumber");
        var notes = string.IsNullOrEmpty(GetFieldValue("Notes")) ? null : GetFieldValue("Notes");

        _paymentResult =
            bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider, referenceNumber, notes);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I record a payment of (.*) on (.*) using (.*)")]
    public void WhenIRecordAPaymentOfOnUsing(decimal amount, string dateString, string methodString)
    {
        var paymentDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var method = Enum.Parse<PaymentMethod>(methodString);

        _paymentResult = bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider);
        Assert.True(_paymentResult.IsSuccess);
    }

    [When("I attempt to record a payment of (.*) on (.*) using (.*)")]
    public void WhenIAttemptToRecordAPaymentOfOnUsing(decimal amount, string dateString, string methodString)
    {
        var paymentDate = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var method = Enum.Parse<PaymentMethod>(methodString);

        _paymentResult = bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I attempt to record a payment with amount (.*)")]
    public void WhenIAttemptToRecordAPaymentWithAmount(decimal amount)
    {
        var paymentDate = DateTime.UtcNow.AddDays(-1);
        _paymentResult = bookingContext.Booking.RecordPayment(amount, paymentDate, PaymentMethod.Cash, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I attempt to record a payment with a date in the future")]
    public void WhenIAttemptToRecordAPaymentWithADateInTheFuture()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, futureDate, PaymentMethod.Cash, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I attempt to record a payment with an invalid payment method")]
    public void WhenIAttemptToRecordAPaymentWithAnInvalidPaymentMethod()
    {
        var paymentDate = DateTime.UtcNow.AddDays(-1);
        _paymentResult = bookingContext.Booking.RecordPayment(100m, paymentDate, (PaymentMethod)999, _timeProvider);
        bookingContext.PaymentResult = _paymentResult;
    }

    [When("I record payments using each payment method:")]
    public void WhenIRecordPaymentsUsingEachPaymentMethod(Table table)
    {
        var allSuccessful = true;
        foreach (var row in table.Rows)
        {
            var method = Enum.Parse<PaymentMethod>(row["Method"]);
            var amount = decimal.Parse(row["Amount"], CultureInfo.InvariantCulture);
            var paymentDate = DateTime.UtcNow.AddDays(-1);

            var result = bookingContext.Booking.RecordPayment(amount, paymentDate, method, _timeProvider);
            if (!result.IsSuccess)
            {
                allSuccessful = false;
                break;
            }
        }

        bookingContext.AllPaymentsSuccessful = allSuccessful;
    }

    [Then("the payment should be recorded successfully")]
    public void ThenThePaymentShouldBeRecordedSuccessfully()
    {
        Assert.True(_paymentResult.IsSuccess);
    }

    [Then(@"the payment should be rejected with error ""(.*)""")]
    public void ThenThePaymentShouldBeRejectedWithError(string expectedError)
    {
        Assert.False(_paymentResult.IsSuccess);
        Assert.Contains(expectedError, _paymentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Then(@"the payment should be rejected with error containing ""(.*)""")]
    public void ThenThePaymentShouldBeRejectedWithErrorContaining(string expectedErrorFragment)
    {
        Assert.False(_paymentResult.IsSuccess);
        Assert.Contains(expectedErrorFragment, _paymentResult.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Then("the amount paid should be (.*)")]
    public void ThenTheAmountPaidShouldBe(decimal expectedAmount)
    {
        Assert.Equal(expectedAmount, bookingContext.Booking.AmountPaid);
    }

    [Then("the remaining balance should be (.*)")]
    public void ThenTheRemainingBalanceShouldBe(decimal expectedBalance)
    {
        Assert.Equal(expectedBalance, bookingContext.Booking.RemainingBalance);
    }

    [Then("the payment reference number should be empty")]
    public void ThenThePaymentReferenceNumberShouldBeEmpty()
    {
        Assert.True(_paymentResult.IsSuccess);
        Assert.Null(_paymentResult.Value.ReferenceNumber);
    }

    [Then("the payment notes should be empty")]
    public void ThenThePaymentNotesShouldBeEmpty()
    {
        Assert.True(_paymentResult.IsSuccess);
        Assert.Null(_paymentResult.Value.Notes);
    }

    [Then("all payments should be recorded successfully")]
    public void ThenAllPaymentsShouldBeRecordedSuccessfully()
    {
        Assert.NotNull(bookingContext.AllPaymentsSuccessful);
        Assert.True(bookingContext.AllPaymentsSuccessful.Value);
    }

    [Given("I record a payment of (.*) on (.*) using (.*)")]
    public void GivenIRecordAPaymentOfOnUsing(decimal amount, string dateString, string methodString)
    {
        WhenIRecordAPaymentOfOnUsing(amount, dateString, methodString);
    }

    [Given(@"the booking payment status is ""(.*)""")]
    public void GivenTheBookingPaymentStatusIs(string expectedStatus)
    {
        var status = EntityBuilders.ParsePaymentStatus(expectedStatus);
        Assert.Equal(status, bookingContext.Booking.PaymentStatus);
    }
}
