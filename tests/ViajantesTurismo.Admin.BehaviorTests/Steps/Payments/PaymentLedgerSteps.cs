using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Payments;

[Binding]
public sealed class PaymentLedgerSteps(TourContext tourContext, BookingContext bookingContext)
{
    private static DateTimeOffset Now => new(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
    private static DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
    private PaymentInstallment? _installment;
    private Guid _scheduleId;
    private Guid _receiptId;
    private Guid? _quoteId;
    private BookingFinancialHistory History => bookingContext.Booking.FinancialHistory.ShouldNotBeNull();
    private PaymentInstallment Installment => _installment.ShouldNotBeNull();

    [Given("an EUR booking priced at 100 with no approved payment terms")]
    public void GivenAnEurBookingWithoutApprovedTerms()
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(BasePrice: 99m, RegularBikePrice: 1m, Currency: Currency.Euro)));
        bookingContext.Booking = tourContext.Tour.AddBooking(TourBookingRequest.CreateSingle(Guid.CreateVersion7(), BikeType.Regular, RoomType.DoubleOccupancy), Now.UtcDateTime).Value;
    }

    [When("the Admin drafts EUR payment terms")]
    public void WhenTheAdminDraftsTerms()
    {
        _installment = PaymentInstallment.Create(100m, Today).Value;
        _scheduleId = tourContext.Tour.DraftBookingPaymentSchedule(bookingContext.Booking.Id, 0, 100m, [Installment], "owner", Now).Value;
    }

    [When("the same Admin approves those payment terms")]
    public void WhenTheAdminApprovesTerms() => tourContext.Tour.ApproveBookingPaymentSchedule(bookingContext.Booking.Id, History.Version,
        _scheduleId, FinancialApproval.Create("owner", Now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();

    [Given("EUR payment terms are approved")]
    public void GivenApprovedTerms()
    {
        WhenTheAdminDraftsTerms();
        WhenTheAdminApprovesTerms();
    }

    [Given("an Admin approves an agreed quote of {decimal} BRL per EUR")]
    public void GivenAnApprovedBrlQuote(decimal rate)
    {
        var quote = AgreedFxQuote.Create(rate, "recipient agreement", "agreed-quote", Now, Now.AddDays(1), Now).Value;
        _quoteId = tourContext.Tour.DraftBookingPaymentQuote(bookingContext.Booking.Id, History.Version, quote, false, "owner", Now).Value;
        tourContext.Tour.ApproveBookingPaymentQuote(bookingContext.Booking.Id, History.Version, _quoteId.Value,
            FinancialApproval.Create("owner", Now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
    }

    [When("an Operator records actual cash of {decimal} in {word}")]
    public void WhenTheOperatorRecordsCash(decimal amount, string currency)
    {
        var settlementCurrency = currency == "BRL" ? Currency.Real : Currency.Euro;
        _receiptId = tourContext.Tour.RecordBookingReceipt(bookingContext.Booking.Id, History.Version,
            ReceiptSettlement.Create(settlementCurrency, amount, 0).Value, Today, Now, PaymentMethod.BankTransfer, "recipient-receipt",
            [InstallmentAllocation.Create(Installment.Id, settlementCurrency == Currency.Euro ? Math.Min(amount, 100m) : 100m).Value],
            "operator", Now, Today).Value;
    }

    [When("the Admin approves EUR credit of {decimal}")]
    public void WhenTheAdminApprovesCredit(decimal credit)
    {
        var reconciliation = tourContext.Tour.DraftBookingReceiptReconciliation(bookingContext.Booking.Id, History.Version, _receiptId,
            credit, _quoteId, [InstallmentAllocation.Create(Installment.Id, Math.Min(credit, 100m)).Value], "owner", Now).Value;
        tourContext.Tour.ApproveBookingReceiptReconciliation(bookingContext.Booking.Id, History.Version, reconciliation,
            FinancialApproval.Create("owner", Now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
    }

    [When("the booking is cancelled without an obligation adjustment")]
    public void WhenTheBookingIsCancelled() => tourContext.Tour.CancelBooking(bookingContext.Booking.Id).IsSuccess.ShouldBeTrue();

    [When("the Admin reverses that erroneous receipt")]
    public void WhenTheAdminReversesTheReceipt() => tourContext.Tour.ReverseBookingReceipt(bookingContext.Booking.Id, History.Version,
        _receiptId, FinancialApproval.Create("owner", Now, FinancialReason.ReceiptError, true).Value).IsSuccess.ShouldBeTrue();

    [Then("the outstanding EUR obligation is {decimal}")]
    public void ThenTheOutstandingObligationIs(decimal amount) => bookingContext.Booking.RemainingBalance.ShouldBe(amount);

    [Then("the approved EUR credit is {decimal}")]
    public void ThenTheApprovedCreditIs(decimal amount) => bookingContext.Booking.AmountPaid.ShouldBe(amount);

    [Then("the unapplied EUR customer credit is {decimal}")]
    public void ThenTheUnappliedCreditIs(decimal amount) => History.UnallocatedEuroCredit.ShouldBe(amount);

    [Then("the schedule is visibly self approved")]
    public void ThenTheScheduleIsSelfApproved()
    {
        var schedule = History.ApprovedSchedule.ShouldNotBeNull();
        schedule.Approval.ShouldNotBeNull().ActorId.ShouldBe(schedule.CreatedBy);
    }

}
