using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class BookingFinancialHistoryTests
{
    [Fact]
    public void Booking_price_does_not_create_debt_before_terms_are_approved()
    {
        // Arrange
        var scenario = new PaymentScenario();

        // Act
        var outstanding = scenario.Booking.RemainingBalance;
        var status = scenario.Booking.PaymentStatus;

        // Assert
        outstanding.ShouldBe(0m);
        status.ShouldBe(PaymentStatus.Unpaid);
    }

    [Fact]
    public void Solo_owner_can_approve_terms_reconcile_excess_refund_excess_correct_and_reverse_without_erasing_history()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var tour = scenario.Tour;
        var booking = scenario.Booking;
        var now = PaymentScenario.Now;
        var installment = PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        var approval = FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value;

        // Act / Assert: the draft is separate from explicit self-approval.
        scenario.History.ApprovedSchedule.ShouldBeNull();
        tour.ApproveBookingPaymentSchedule(booking.Id, scenario.History.Version, schedule, approval).IsSuccess.ShouldBeTrue();
        scenario.History.ApprovedSchedule.ShouldNotBeNull();
        scenario.History.ApprovedSchedule.SelfApproved.ShouldBeTrue();
        var receipt = tour.RecordBookingReceipt(booking.Id, scenario.History.Version, ReceiptSettlement.Create(Currency.Euro, 120m, 0m).Value,
            new DateOnly(2030, 1, 5), now, PaymentMethod.BankTransfer, "transfer", [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now, new DateOnly(2030, 1, 5)).Value;
        booking.AmountPaid.ShouldBe(0);
        var reconciliation = tour.DraftBookingReceiptReconciliation(booking.Id, scenario.History.Version, receipt, 120m, null,
            [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now).Value;
        booking.AmountPaid.ShouldBe(0);
        tour.ApproveBookingReceiptReconciliation(booking.Id, scenario.History.Version, reconciliation,
            FinancialApproval.Create("owner", now, FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        booking.RemainingBalance.ShouldBe(0);
        scenario.History.UnallocatedEuroCredit.ShouldBe(20m);
        var refund = tour.DraftBookingPaymentRefund(booking.Id, scenario.History.Version, receipt, 20m, 20m, 20m, null, [], "excess-return", now, "owner", now).Value;
        scenario.History.UnallocatedEuroCredit.ShouldBe(20m);
        tour.ApproveBookingPaymentRefund(booking.Id, scenario.History.Version, refund,
            FinancialApproval.Create("owner", now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        booking.RemainingBalance.ShouldBe(0);
        scenario.History.UnallocatedEuroCredit.ShouldBe(0);
        tour.ReverseBookingReceipt(booking.Id, scenario.History.Version, receipt,
            FinancialApproval.Create("owner", now, FinancialReason.ReceiptError, true).Value).IsFailure.ShouldBeTrue();
        tour.CorrectBookingPaymentRefund(booking.Id, scenario.History.Version, refund,
            FinancialApproval.Create("owner", now, FinancialReason.RefundRecordError, true).Value).IsSuccess.ShouldBeTrue();
        tour.ReverseBookingReceipt(booking.Id, scenario.History.Version, receipt,
            FinancialApproval.Create("owner", now, FinancialReason.ReceiptError, true).Value).IsSuccess.ShouldBeTrue();
        booking.AmountPaid.ShouldBe(0);
        booking.RemainingBalance.ShouldBe(100m);
        scenario.History.ExportState().Receipts.Length.ShouldBe(1);
        scenario.History.ExportState().Refunds.Length.ShouldBe(1);
        scenario.History.ExportState().RefundCorrections.Length.ShouldBe(1);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Replacement_terms_preserve_historical_allocations_and_require_explicit_release_and_selection()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var booking = scenario.Booking;
        var original = PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value;
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [original], "first", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("second", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var receipt = scenario.Tour.RecordBookingReceipt(booking.Id, scenario.History.Version, ReceiptSettlement.Create(Currency.Euro, 100m, 0m).Value,
            new DateOnly(2030, 1, 5), now, PaymentMethod.BankTransfer, "original", [InstallmentAllocation.Create(original.Id, 100m).Value], "first", now, new DateOnly(2030, 1, 5)).Value;
        var reconciliation = scenario.Tour.DraftBookingReceiptReconciliation(booking.Id, scenario.History.Version, receipt, 100m, null,
            [InstallmentAllocation.Create(original.Id, 100m).Value], "first", now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(booking.Id, scenario.History.Version, reconciliation,
            FinancialApproval.Create("second", now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var historicalAllocation = scenario.History.ExportState().Allocations.ShouldHaveSingleItem();

        // Act
        var replacement = PaymentInstallment.Create(80m, PaymentScenario.DueDate).Value;
        var revision = scenario.Tour.DraftBookingPaymentSchedule(booking.Id, scenario.History.Version, 80m, [replacement], "second", now).Value;
        scenario.History.CanInstruct(booking.TotalPrice, booking.Status).ShouldBeFalse();
        scenario.Tour.ApproveBookingPaymentSchedule(booking.Id, scenario.History.Version, revision,
            FinancialApproval.Create("first", now, FinancialReason.TermsChanged, true).Value).IsSuccess.ShouldBeTrue();

        // Assert: no automatic allocation migration or deletion.
        booking.AmountPaid.ShouldBe(0);
        booking.RemainingBalance.ShouldBe(80m);
        scenario.History.ExportState().Allocations.ShouldHaveSingleItem().Id.ShouldBe(historicalAllocation.Id);
        scenario.Tour.ReleaseBookingPaymentAllocations(booking.Id, scenario.History.Version, [InstallmentAllocation.Create(historicalAllocation.Id, 100m).Value],
            FinancialApproval.Create("second", now, FinancialReason.AllocationSelected, true).Value).IsSuccess.ShouldBeTrue();
        scenario.Tour.AllocateBookingPayment(booking.Id, scenario.History.Version, receipt, [InstallmentAllocation.Create(replacement.Id, 80m).Value],
            FinancialApproval.Create("first", now, FinancialReason.AllocationSelected, true).Value).IsSuccess.ShouldBeTrue();
        booking.RemainingBalance.ShouldBe(0);
        scenario.History.UnallocatedEuroCredit.ShouldBe(20m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Cancellation_does_not_erase_debt_and_zero_obligation_requires_an_explicit_replacement_approval()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 100m,
            [PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value], "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();

        // Act
        scenario.Tour.CancelBooking(scenario.Booking.Id).IsSuccess.ShouldBeTrue();

        // Assert
        scenario.Booking.RemainingBalance.ShouldBe(100m);
        scenario.History.CanInstruct(scenario.Booking.TotalPrice, scenario.Booking.Status).ShouldBeFalse();
        var adjusted = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, 0m, [], "owner", now).Value;
        scenario.Booking.RemainingBalance.ShouldBe(100m);
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, adjusted,
            FinancialApproval.Create("owner", now, FinancialReason.CancellationAdjustment, true).Value).IsSuccess.ShouldBeTrue();
        scenario.Booking.RemainingBalance.ShouldBe(0);
        scenario.Booking.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        scenario.Tour.Delete().IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Independent_reviews_are_revision_bound_and_requests_for_changes_do_not_revoke_approval()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 100m,
            [PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value], "first", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("first", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var originalVersion = scenario.History.Version;

        // Act
        var creatorReview = scenario.Tour.ReviewBookingFinancialRecord(scenario.Booking.Id, originalVersion, schedule, false, "first", now);
        var independent = scenario.Tour.ReviewBookingFinancialRecord(scenario.Booking.Id, originalVersion, schedule, true, "second", now);
        var staleChange = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, originalVersion, 100m,
            [PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value], "first", now);

        // Assert
        creatorReview.IsFailure.ShouldBeTrue();
        independent.IsSuccess.ShouldBeTrue();
        staleChange.IsFailure.ShouldBeTrue();
        scenario.History.ApprovedSchedule.ShouldNotBeNull();
        scenario.History.ApprovedSchedule.Id.ShouldBe(schedule);
        scenario.History.CanInstruct(scenario.Booking.TotalPrice, scenario.Booking.Status).ShouldBeTrue();
        scenario.History.ExportState().Reviews.ShouldHaveSingleItem().TargetRevision.ShouldBe(1);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Overdue_status_uses_calendar_business_dates_and_does_not_change_payment_state()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 100m,
            [PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value], "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();

        // Act / Assert
        scenario.History.IsOverdue(PaymentScenario.DueDate.AddDays(-1)).ShouldBeFalse();
        scenario.History.IsOverdue(PaymentScenario.DueDate).ShouldBeFalse();
        scenario.History.IsOverdue(PaymentScenario.DueDate.AddDays(1)).ShouldBeTrue();
        scenario.Booking.PaymentStatus.ShouldBe(PaymentStatus.Unpaid);
    }

    [Theory]
    [InlineData(0.5, 0.02)]
    [InlineData(5.55555555, 0.17)]
    public void Frozen_instructions_round_away_from_zero_and_assign_every_installment_remainder(decimal rate, decimal expectedTotal)
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var installments = new[] { PaymentInstallment.Create(0.01m, PaymentScenario.DueDate).Value, PaymentInstallment.Create(0.01m, PaymentScenario.DueDate).Value, PaymentInstallment.Create(0.01m, PaymentScenario.DueDate).Value };
        var schedule = scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 0.03m, installments, "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentSchedule(scenario.Booking.Id, scenario.History.Version, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        var quote = scenario.Tour.DraftBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version,
            AgreedFxQuote.Create(rate, "agreed", "manual-reference", now, now.AddHours(1), now).Value, false, "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version, quote,
            FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        var account = PaymentAccount.Create("Pix", PaymentRecipientDetails.CreatePix("Recipient", PixKeyType.Email, "payment@example.com").Value, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();

        // Act
        var frozen = scenario.Tour.CreateBookingPaymentInstruction(scenario.Booking.Id, account, installments.Select(item => item.Id).ToArray(), quote, "reference", now).Value;

        // Assert
        frozen.SettlementAmount.ShouldBe(expectedTotal);
        frozen.ExportState().Installments.Sum(item => item.SettlementAmount).ShouldBe(expectedTotal);
        frozen.ExportState().Installments.All(item => item.SettlementAmount >= 0).ShouldBeTrue();
        FrozenPaymentInstruction.Restore(frozen.ExportState()).IsSuccess.ShouldBeTrue();
        scenario.Tour.CanDeliverBookingPaymentInstruction(frozen, account, now.AddHours(1).AddTicks(-1)).ShouldBeTrue();
        scenario.Tour.CanDeliverBookingPaymentInstruction(frozen, account, now.AddHours(1)).ShouldBeFalse();
        scenario.Tour.CanDeliverBookingPaymentInstruction(frozen, account, now.AddHours(1).AddTicks(1)).ShouldBeFalse();
    }
    [Fact]
    public void Pricing_update_cannot_change_the_currency_of_existing_eur_obligations()
    {
        // Arrange
        var scenario = new PaymentScenario();
        scenario.ApproveSchedule();
        var original = scenario.Tour.Pricing;

        // Act
        var changed = scenario.Tour.UpdatePricing(original.SingleRoomSupplementPrice, original.RegularBikePrice, original.EBikePrice, Currency.Real);

        // Assert
        changed.IsFailure.ShouldBeTrue();
        scenario.Tour.Pricing.Currency.ShouldBe(Currency.Euro);
        scenario.History.ApprovedSchedule.ShouldNotBeNull();
        scenario.History.ApprovedSchedule.Obligation.Amount.ShouldBe(100m);
    }
    [Fact]
    public void Receipts_against_draft_terms_require_explicit_proposed_allocations_without_approving_the_terms()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var now = PaymentScenario.Now;
        var installment = PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value;
        scenario.Tour.DraftBookingPaymentSchedule(scenario.Booking.Id, 0, 100m, [installment], "owner", now).IsSuccess.ShouldBeTrue();
        var settlement = ReceiptSettlement.Create(Currency.Euro, 100m, 0m).Value;

        // Act
        var unselected = scenario.Tour.RecordBookingReceipt(scenario.Booking.Id, scenario.History.Version, settlement,
            new DateOnly(2030, 1, 5), now, PaymentMethod.BankTransfer, "unselected", [], "operator", now, new DateOnly(2030, 1, 5));
        var selected = scenario.Tour.RecordBookingReceipt(scenario.Booking.Id, scenario.History.Version, settlement,
            new DateOnly(2030, 1, 5), now, PaymentMethod.BankTransfer, "selected", [InstallmentAllocation.Create(installment.Id, 100m).Value], "operator", now, new DateOnly(2030, 1, 5));

        // Assert
        unselected.IsFailure.ShouldBeTrue();
        selected.IsSuccess.ShouldBeTrue();
        scenario.History.ApprovedSchedule.ShouldBeNull();
        scenario.Booking.AmountPaid.ShouldBe(0m);
        scenario.History.IsReceiptPending(selected.Value).ShouldBeTrue();
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }
}
