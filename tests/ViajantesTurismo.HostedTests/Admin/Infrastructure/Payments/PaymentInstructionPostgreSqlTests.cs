using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentInstructionPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Instruction_snapshot_survives_restart_with_submicrosecond_timestamps_and_self_approval(bool brl)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        scenario.Clock.CurrentInstant = scenario.Clock.CurrentInstant.AddTicks(7);
        var now = scenario.Clock.GetUtcNow();
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Euro)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        var installment = PaymentInstallment.Create(100m, new DateOnly(2030, 1, 10)).Value;
        var schedule = tour.DraftBookingPaymentSchedule(booking.Id, 0, 100m, [installment], "owner", now).Value;
        tour.ApproveBookingPaymentSchedule(booking.Id, booking.FinancialHistory?.Version ?? 0, schedule,
            FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        Guid? quoteId = null;
        if (brl)
        {
            quoteId = tour.DraftBookingPaymentQuote(booking.Id, booking.FinancialHistory?.Version ?? 0,
                AgreedFxQuote.Create(5.55555555m, "recipient", "agreed-bank-reference", now, now.AddHours(1), now).Value, false, "owner", now).Value;
            tour.ApproveBookingPaymentQuote(booking.Id, booking.FinancialHistory?.Version ?? 0, quoteId.Value,
                FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        }
        var recipient = brl ? PaymentRecipientDetails.CreatePix("Recipient", PandaBear.BuildingBlocks.PixKeyType.Email, "payment@example.test").Value
            : PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value;
        var account = PaymentAccount.Create("Recipient", recipient, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        Guid documentId;
        await using (var first = scenario.Open())
        {
            await first.Migrate(ct);
            await first.Seed(tour, ct);
            await first.SeedAccount(account, ct);
            var generated = await first.GeneratePaymentInstructions.Handle(new GeneratePaymentInstructionsCommand(booking.Id, null,
                new GeneratePaymentInstructionsDto
                {
                    ExpectedVersion = booking.FinancialHistory?.Version ?? 0,
                    AccountId = account.Id,
                    ExpectedAccountVersion = account.Version,
                    QuoteId = quoteId,
                    InstallmentIds = [installment.Id]
                }, "owner", "instruction-generate"), ct);
            generated.IsSuccess.ShouldBeTrue();
            documentId = generated.Value;
        }

        // Act
        await using var restarted = scenario.Open();
        var instruction = await restarted.Instructions.GetById(documentId, ct);

        // Assert
        instruction.ShouldNotBeNull();
        instruction.Approval.CreatedAt.ShouldBe(now);
        instruction.Eligible.ShouldBeTrue();
        var approved = await restarted.ApprovePaymentInstructions.Handle(new ApprovePaymentInstructionsCommand(documentId,
            new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = instruction.DocumentVersion, Confirmed = true, Reason = FinancialReasonDto.InstructionApproved }, "owner", "instruction-approve"), ct);
        approved.IsSuccess.ShouldBeTrue();
        var current = await restarted.Instructions.GetById(documentId, ct);
        current.ShouldNotBeNull();
        current.Approval.SelfApproved.ShouldBeTrue();
        var replay = await restarted.GeneratePaymentInstructions.Handle(new GeneratePaymentInstructionsCommand(booking.Id, null,
            new GeneratePaymentInstructionsDto
            {
                ExpectedVersion = booking.FinancialHistory?.Version ?? 0,
                AccountId = account.Id,
                ExpectedAccountVersion = account.Version,
                QuoteId = quoteId,
                InstallmentIds = [installment.Id]
            }, "owner", "instruction-generate"), ct);
        replay.IsSuccess.ShouldBeTrue();
        replay.Value.ShouldBe(documentId);
        var duplicateGeneration = await restarted.GeneratePaymentInstructions.Handle(new GeneratePaymentInstructionsCommand(booking.Id, null,
            new GeneratePaymentInstructionsDto
            {
                ExpectedVersion = booking.FinancialHistory?.Version ?? 0,
                AccountId = account.Id,
                ExpectedAccountVersion = account.Version,
                QuoteId = quoteId,
                InstallmentIds = [installment.Id]
            }, "owner", "different-generation-key"), ct);
        duplicateGeneration.IsFailure.ShouldBeTrue();
        var finalized = await restarted.FinalizePaymentInstructions.Handle(new FinalizePaymentInstructionsCommand(documentId,
            new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = current.DocumentVersion, Confirmed = true, Reason = FinancialReasonDto.InstructionFinalized }, "owner", "instruction-finalize"), ct);
        finalized.IsSuccess.ShouldBeTrue();
        var downloaded = await restarted.Instructions.Download(documentId, "owner", ct);
        downloaded.IsSuccess.ShouldBeTrue();
        var html = global::System.Text.Encoding.UTF8.GetString(downloaded.Value.Content.Span);
        html.ShouldContain(brl ? "payment@example.test" : "DE89370400440532013000", StringComparison.Ordinal);
        html.ShouldContain("100", StringComparison.Ordinal);
        if (brl)
        {
            scenario.Clock.CurrentInstant = now.AddHours(1).AddTicks(-1);
            scenario.Clock.AutoAdvance = TimeSpan.FromTicks(1);
            var crossedExpiry = await restarted.Instructions.Download(documentId, "owner", ct);
            crossedExpiry.IsFailure.ShouldBeTrue();
            scenario.Clock.AutoAdvance = TimeSpan.Zero;
        }
        if (brl)
        {
            var beforeReceipt = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeReceipt.ShouldNotBeNull();
            var receipt = await restarted.RecordBookingReceipt.Handle(new RecordBookingReceiptCommand(booking.Id,
                new CreateBookingReceiptDto
                {
                    ExpectedVersion = beforeReceipt.Version,
                    SettlementCurrency = CurrencyDto.Real,
                    Gross = 555.56m,
                    Fees = 0m,
                    PaymentDate = new DateOnly(2030, 1, 5),
                    SettledAt = now,
                    Method = PaymentMethodDto.BankTransfer,
                    Reference = current.PaymentReference,
                    ProposedAllocations = [new InstallmentAllocationDto { TargetId = installment.Id, EuroAmount = 100m }]
                }, "owner", "brl-receipt"), ct);
            receipt.IsSuccess.ShouldBeTrue();
            var beforeReconciliation = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeReconciliation.ShouldNotBeNull();
            beforeReconciliation.OutstandingEuro.ShouldBe(100m);
            var reconciliation = await restarted.DraftBookingReceiptReconciliation.Handle(new DraftBookingReceiptReconciliationCommand(booking.Id, receipt.Value,
                new CreateReceiptReconciliationDto
                {
                    ExpectedVersion = beforeReconciliation.Version,
                    EuroCredit = 100m,
                    QuoteId = quoteId,
                    InstructionId = documentId,
                    Allocations = [new InstallmentAllocationDto { TargetId = installment.Id, EuroAmount = 100m }]
                }, "owner", "brl-reconciliation"), ct);
            reconciliation.IsSuccess.ShouldBeTrue();
            var beforeApproval = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeApproval.ShouldNotBeNull();
            var exact = await restarted.ApproveBookingReceiptReconciliation.Handle(new ApproveBookingReceiptReconciliationCommand(booking.Id, reconciliation.Value,
                new ApproveFinancialRecordDto { ExpectedVersion = beforeApproval.Version, Confirmed = true, Reason = FinancialReasonDto.ExactReceipt }, "owner", "brl-exact"), ct);
            exact.IsSuccess.ShouldBeTrue();
            var paid = await restarted.Financials.GetByBookingId(booking.Id, ct);
            paid.ShouldNotBeNull();
            paid.OutstandingEuro.ShouldBe(0m);
            var refundAgreedAt = scenario.Clock.GetUtcNow();
            var refundQuote = await restarted.DraftBookingPaymentQuote.Handle(new DraftBookingPaymentQuoteCommand(booking.Id,
                new CreatePaymentQuoteDto
                {
                    ExpectedVersion = paid.Version,
                    BrlPerEuro = 6m,
                    Source = "recipient",
                    Reference = "new refund agreement",
                    ForRefund = true,
                    AgreedAt = refundAgreedAt,
                    ExpiresAt = refundAgreedAt.AddHours(1)
                }, "owner", "brl-refund-quote"), ct);
            refundQuote.IsSuccess.ShouldBeTrue();
            var beforeQuoteApproval = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeQuoteApproval.ShouldNotBeNull();
            var quoteApproved = await restarted.ApproveBookingPaymentQuote.Handle(new ApproveBookingPaymentQuoteCommand(booking.Id, refundQuote.Value,
                new ApproveFinancialRecordDto { ExpectedVersion = beforeQuoteApproval.Version, Confirmed = true, Reason = FinancialReasonDto.QuoteAgreed }, "owner", "brl-refund-quote-approved"), ct);
            quoteApproved.IsSuccess.ShouldBeTrue();
            var beforeRefund = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeRefund.ShouldNotBeNull();
            var refund = await restarted.DraftBookingPaymentRefund.Handle(new DraftBookingPaymentRefundCommand(booking.Id, receipt.Value,
                new CreatePaymentRefundDto
                {
                    ExpectedVersion = beforeRefund.Version,
                    OriginalCashBasis = 555.56m,
                    EuroCreditReversed = 100m,
                    EuroCashPayable = 100m,
                    ActualSettlement = 600m,
                    RefundQuoteId = refundQuote.Value,
                    Reference = "already performed refund",
                    PerformedAt = refundAgreedAt,
                    ReversedAllocations = [new InstallmentAllocationDto { TargetId = beforeRefund.Allocations.ShouldHaveSingleItem().Id, EuroAmount = 100m }]
                }, "owner", "brl-refund"), ct);
            refund.IsSuccess.ShouldBeTrue();
            var beforeRefundApproval = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeRefundApproval.ShouldNotBeNull();
            var refundApproved = await restarted.ApproveBookingPaymentRefund.Handle(new ApproveBookingPaymentRefundCommand(booking.Id, refund.Value,
                new ApproveFinancialRecordDto { ExpectedVersion = beforeRefundApproval.Version, Confirmed = true, Reason = FinancialReasonDto.RefundPerformed }, "owner", "brl-refund-approved"), ct);
            refundApproved.IsSuccess.ShouldBeTrue();
            var refunded = await restarted.Financials.GetByBookingId(booking.Id, ct);
            refunded.ShouldNotBeNull();
            refunded.OutstandingEuro.ShouldBe(100m);
            refunded.Refunds.ShouldHaveSingleItem().ActualSettlement.ShouldBe(600m);
            refunded.Refunds.ShouldHaveSingleItem().FxDifference.ShouldBe(44.44m);
        }
        if (!brl)
        {
            scenario.Clock.CurrentInstant = now.AddDays(6);
            var beforeReceipt = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeReceipt.ShouldNotBeNull();
            var receipt = await restarted.RecordBookingReceipt.Handle(new RecordBookingReceiptCommand(booking.Id,
                new CreateBookingReceiptDto
                {
                    ExpectedVersion = beforeReceipt.Version,
                    SettlementCurrency = CurrencyDto.Euro,
                    Gross = 100m,
                    Fees = 0m,
                    PaymentDate = new DateOnly(2030, 1, 5),
                    SettledAt = scenario.Clock.GetUtcNow(),
                    Method = PaymentMethodDto.BankTransfer,
                    Reference = current.PaymentReference,
                    ProposedAllocations = [new InstallmentAllocationDto { TargetId = installment.Id, EuroAmount = 100m }]
                }, "owner", "late-receipt"), ct);
            receipt.IsSuccess.ShouldBeTrue();
            var beforeReconciliation = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeReconciliation.ShouldNotBeNull();
            var reconciliation = await restarted.DraftBookingReceiptReconciliation.Handle(new DraftBookingReceiptReconciliationCommand(booking.Id, receipt.Value,
                new CreateReceiptReconciliationDto
                {
                    ExpectedVersion = beforeReconciliation.Version,
                    EuroCredit = 100m,
                    QuoteId = null,
                    InstructionId = documentId,
                    Allocations = [new InstallmentAllocationDto { TargetId = installment.Id, EuroAmount = 100m }]
                }, "owner", "late-reconciliation"), ct);
            reconciliation.IsSuccess.ShouldBeTrue();
            var beforeApproval = await restarted.Financials.GetByBookingId(booking.Id, ct);
            beforeApproval.ShouldNotBeNull();
            var exact = await restarted.ApproveBookingReceiptReconciliation.Handle(new ApproveBookingReceiptReconciliationCommand(booking.Id, reconciliation.Value,
                new ApproveFinancialRecordDto { ExpectedVersion = beforeApproval.Version, Confirmed = true, Reason = FinancialReasonDto.ExactReceipt }, "owner", "incorrect-exact"), ct);
            exact.IsFailure.ShouldBeTrue();
            var late = await restarted.ApproveBookingReceiptReconciliation.Handle(new ApproveBookingReceiptReconciliationCommand(booking.Id, reconciliation.Value,
                new ApproveFinancialRecordDto { ExpectedVersion = beforeApproval.Version, Confirmed = true, Reason = FinancialReasonDto.LateReceipt }, "owner", "explicit-late"), ct);
            late.IsSuccess.ShouldBeTrue();
        }
        var retirement = await restarted.RetirePaymentAccount.Handle(new RetirePaymentAccountCommand(account.Id,
            new RetirePaymentAccountDto { ExpectedVersion = account.Version, Confirmed = true, Reason = PaymentAccountReasonDto.NoLongerUsed }, "owner", "retire-after-download"), ct);
        retirement.IsSuccess.ShouldBeTrue();
        var blocked = await restarted.Instructions.Download(documentId, "owner", ct);
        blocked.IsFailure.ShouldBeTrue();
    }
}
