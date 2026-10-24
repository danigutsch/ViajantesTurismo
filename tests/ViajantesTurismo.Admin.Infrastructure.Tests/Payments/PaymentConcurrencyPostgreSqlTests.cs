using System.Text;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentConcurrencyPostgreSqlTests(PostgreSqlTestServerFixture fixture)
{
    [Fact]
    public async Task Concurrent_receipts_from_the_same_financial_revision_commit_only_one_mutation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var source = await scenario.SeedCustomerCredit(ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        var history = await first.Financials.GetByBookingId(source.BookingId, ct);
        history.ShouldNotBeNull();
        var input = new CreateBookingReceiptDto
        {
            ExpectedVersion = history.Version,
            SettlementCurrency = CurrencyDto.Euro,
            Gross = 100m,
            Fees = 0m,
            PaymentDate = new DateOnly(2030, 1, 5),
            SettledAt = scenario.Clock.GetUtcNow(),
            Method = PaymentMethodDto.BankTransfer,
            Reference = "receipt-race",
            ProposedAllocations = [new InstallmentAllocationDto { TargetId = source.InstallmentId, EuroAmount = 100m }]
        };
        var one = new RecordBookingReceiptCommand(source.BookingId, input, "first", "record-first");
        var two = new RecordBookingReceiptCommand(source.BookingId, input, "second", "record-second");

        // Act
        var results = await Task.WhenAll(first.RecordBookingReceipt.Handle(one, ct), second.RecordBookingReceipt.Handle(two, ct));

        // Assert
        results.Count(result => result.IsSuccess).ShouldBe(1);
        results.Count(result => result.IsFailure).ShouldBe(1);
        var current = await first.Financials.GetByBookingId(source.BookingId, ct);
        current.ShouldNotBeNull();
        current.Version.ShouldBe(history.Version + 1);
        current.Receipts.Count.ShouldBe(history.Receipts.Count + 1);
        current.OutstandingEuro.ShouldBe(history.OutstandingEuro);
    }

    [Fact]
    public async Task Concurrent_duplicate_commands_commit_one_encrypted_account_and_one_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        await first.Migrate(ct);
        var command = new CreatePaymentAccountCommand(new CreatePaymentAccountDto
        {
            Label = "EUR",
            Recipient = new PaymentRecipientDto
            {
                Kind = PaymentAccountKindDto.Iban,
                SettlementCurrency = CurrencyDto.Euro,
                RecipientName = "Recipient",
                Iban = "DE89370400440532013000",
                Bic = "COBADEFFXXX"
            }
        }, "owner", "same-command");

        // Act
        var results = await Task.WhenAll(first.CreatePaymentAccount.Handle(command, ct), second.CreatePaymentAccount.Handle(command, ct));

        // Assert
        results.All(result => result.IsSuccess).ShouldBeTrue();
        results[0].Value.ShouldBe(results[1].Value);
        var assertionTarget1 = await first.AccountCount(ct);
        assertionTarget1.ShouldBe(1);
        var assertionTarget2 = await first.AuditCount(ct);
        assertionTarget2.ShouldBe(1);
        var ciphertext = await first.AccountCiphertext(results[0].Value, ct);
        Encoding.UTF8.GetString(ciphertext).ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
        var mismatch = command with
        {
            Input = command.Input with
            {
                Label = "changed input"
            }
        };
        var rejected = await second.CreatePaymentAccount.Handle(mismatch, ct);
        rejected.IsFailure.ShouldBeTrue();
        var assertionTarget3 = await first.AccountCount(ct);
        assertionTarget3.ShouldBe(1);
    }

    [Fact]
    public async Task Two_admins_cannot_verify_and_retire_the_same_account_revision_concurrently()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        await first.Migrate(ct);
        var now = scenario.Clock.GetUtcNow();
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "first", now).Value;
        await first.SeedAccount(account, ct);
        var verify = new VerifyPaymentAccountCommand(account.Id, new VerifyPaymentAccountDto
        {
            ExpectedVersion = account.Version,
            Confirmed = true,
            IndependentCheckAttested = true,
            Method = PaymentAccountVerificationMethodDto.BankStatement,
            CheckedAt = now
        }, "second", "verify");
        var retire = new RetirePaymentAccountCommand(account.Id, new RetirePaymentAccountDto
        {
            ExpectedVersion = account.Version,
            Confirmed = true,
            Reason = PaymentAccountReasonDto.NoLongerUsed
        }, "first", "retire");

        // Act
        var results = await Task.WhenAll(first.VerifyPaymentAccount.Handle(verify, ct), second.RetirePaymentAccount.Handle(retire, ct));

        // Assert
        results.Count(result => result.IsSuccess).ShouldBe(1);
        results.Count(result => result.IsFailure).ShouldBe(1);
        var current = await first.Accounts.GetById(account.Id, ct);
        current.ShouldNotBeNull();
        current.Version.ShouldBe(account.Version + 1);
        var assertionTarget4 = await first.AuditCount(ct);
        assertionTarget4.ShouldBe(2);
    }
    [Fact]
    public async Task Concurrent_allocations_cannot_overallocate_the_same_outstanding_installment()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var source = await scenario.SeedCustomerCredit(ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        var history = await first.Financials.GetByBookingId(source.BookingId, ct);
        history.ShouldNotBeNull();
        var input = new AllocatePaymentCreditDto
        {
            ExpectedVersion = history.Version,
            Confirmed = true,
            Reason = FinancialReasonDto.AllocationSelected,
            Allocations = [new InstallmentAllocationDto { TargetId = source.InstallmentId, EuroAmount = 100m }]
        };
        var one = new AllocateBookingPaymentCommand(source.BookingId, source.ReceiptId, input, "first", "allocate-first");
        var two = new AllocateBookingPaymentCommand(source.BookingId, source.ReceiptId, input, "second", "allocate-second");

        // Act
        var results = await Task.WhenAll(first.AllocateBookingPayment.Handle(one, ct), second.AllocateBookingPayment.Handle(two, ct));

        // Assert
        results.Count(result => result.IsSuccess).ShouldBe(1);
        results.Count(result => result.IsFailure).ShouldBe(1);
        var current = await first.Financials.GetByBookingId(source.BookingId, ct);
        current.ShouldNotBeNull();
        current.OutstandingEuro.ShouldBe(0m);
        current.Allocations.Sum(item => item.ActiveEuroCredit).ShouldBe(100m);
        current.UnallocatedEuroCredit.ShouldBe(20m);
    }

    [Fact]
    public async Task Concurrent_refund_approvals_cannot_return_the_same_customer_credit_twice()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var source = await scenario.SeedCustomerCredit(ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        var history = await first.Financials.GetByBookingId(source.BookingId, ct);
        history.ShouldNotBeNull();
        var input = new CreatePaymentRefundDto
        {
            ExpectedVersion = history.Version,
            OriginalCashBasis = 120m,
            EuroCreditReversed = 120m,
            EuroCashPayable = 120m,
            ActualSettlement = 120m,
            Reference = "already-performed",
            PerformedAt = scenario.Clock.GetUtcNow(),
            ReversedAllocations = []
        };
        var draftOne = await first.DraftBookingPaymentRefund.Handle(new DraftBookingPaymentRefundCommand(source.BookingId, source.ReceiptId, input, "first", "refund-draft-one"), ct);
        draftOne.IsSuccess.ShouldBeTrue();
        var refreshed = await first.Financials.GetByBookingId(source.BookingId, ct);
        refreshed.ShouldNotBeNull();
        var draftTwo = await second.DraftBookingPaymentRefund.Handle(new DraftBookingPaymentRefundCommand(source.BookingId, source.ReceiptId,
            input with
            {
                ExpectedVersion = refreshed.Version,
                Reference = "second-performed"
            }, "second", "refund-draft-two"), ct);
        draftTwo.IsSuccess.ShouldBeTrue();
        var latest = await first.Financials.GetByBookingId(source.BookingId, ct);
        latest.ShouldNotBeNull();
        var approval = new ApproveFinancialRecordDto { ExpectedVersion = latest.Version, Confirmed = true, Reason = FinancialReasonDto.RefundPerformed };

        // Act
        var results = await Task.WhenAll(first.ApproveBookingPaymentRefund.Handle(new ApproveBookingPaymentRefundCommand(source.BookingId, draftOne.Value, approval, "first", "refund-approve-one"), ct),
            second.ApproveBookingPaymentRefund.Handle(new ApproveBookingPaymentRefundCommand(source.BookingId, draftTwo.Value, approval, "second", "refund-approve-two"), ct));

        // Assert
        results.Count(result => result.IsSuccess).ShouldBe(1);
        results.Count(result => result.IsFailure).ShouldBe(1);
        var current = await first.Financials.GetByBookingId(source.BookingId, ct);
        current.ShouldNotBeNull();
        current.Refunds.Count(item => item.Approval.ApprovedAt is not null).ShouldBe(1);
        current.Receipts.ShouldHaveSingleItem().RefundableCashBasis.ShouldBe(0m);
        current.UnallocatedEuroCredit.ShouldBe(0m);
    }
    [Fact]
    public async Task Rejected_mutation_rolls_back_idempotency_ownership_and_creates_no_audit()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var now = scenario.Clock.GetUtcNow();
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", now).Value;
        await session.SeedAccount(account, ct);
        var input = new VerifyPaymentAccountDto
        {
            ExpectedVersion = account.Version,
            Confirmed = false,
            IndependentCheckAttested = true,
            CheckedAt = now,
            Method = PaymentAccountVerificationMethodDto.BankStatement
        };

        // Act
        var rejected = await session.VerifyPaymentAccount.Handle(new VerifyPaymentAccountCommand(account.Id, input, "owner", "rollback-proof"), ct);
        var accepted = await session.VerifyPaymentAccount.Handle(new VerifyPaymentAccountCommand(account.Id, input with
        {
            Confirmed = true
        }, "owner", "rollback-proof"), ct);

        // Assert
        rejected.IsFailure.ShouldBeTrue();
        accepted.IsSuccess.ShouldBeTrue();
        var assertionTarget5 = await session.AuditCount(ct);
        assertionTarget5.ShouldBe(2);
        var current = await session.Accounts.GetById(account.Id, ct);
        current.ShouldNotBeNull();
        current.Version.ShouldBe(account.Version + 1);
        current.SelfApproved.ShouldBeTrue();
    }
    [Fact]
    public async Task Account_retirement_serializes_with_instruction_generation_and_blocks_further_approval()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var source = await scenario.SeedCustomerCredit(ct);
        await using var first = scenario.Open();
        await using var second = scenario.Open();
        var now = scenario.Clock.GetUtcNow();
        var account = PaymentAccount.Create("EUR", PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        await first.SeedAccount(account, ct);
        var history = await first.Financials.GetByBookingId(source.BookingId, ct);
        history.ShouldNotBeNull();
        var input = new GeneratePaymentInstructionsDto
        {
            ExpectedVersion = history.Version,
            AccountId = account.Id,
            ExpectedAccountVersion = account.Version,
            QuoteId = null,
            InstallmentIds = [source.InstallmentId]
        };

        // Act
        var generation = first.GeneratePaymentInstructions.Handle(new GeneratePaymentInstructionsCommand(source.BookingId, null, input, "first", "generation-race"), ct);
        var retirement = second.RetirePaymentAccount.Handle(new RetirePaymentAccountCommand(account.Id,
            new RetirePaymentAccountDto { ExpectedVersion = account.Version, Confirmed = true, Reason = PaymentAccountReasonDto.NoLongerUsed }, "second", "retirement-race"), ct);
        await Task.WhenAll(generation, retirement);

        // Assert
        var retirementResult = await retirement;
        var generationResult = await generation;
        retirementResult.IsSuccess.ShouldBeTrue();
        var retired = await second.Accounts.GetById(account.Id, ct);
        retired.ShouldNotBeNull();
        retired.Status.ShouldBe(PaymentAccountStatusDto.Inactive);
        if (generationResult.IsSuccess)
        {
            var instruction = await first.Instructions.GetById(generationResult.Value, ct);
            instruction.ShouldNotBeNull();
            instruction.Eligible.ShouldBeFalse();
            var approval = await first.ApprovePaymentInstructions.Handle(new ApprovePaymentInstructionsCommand(instruction.Document.Id,
                new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = instruction.DocumentVersion, Confirmed = true, Reason = FinancialReasonDto.InstructionApproved }, "first", "approve-retired-source"), ct);
            approval.IsFailure.ShouldBeTrue();
        }
        var blocked = await first.GeneratePaymentInstructions.Handle(new GeneratePaymentInstructionsCommand(source.BookingId, null,
            input with
            {
                ExpectedAccountVersion = retired.Version
            }, "first", "generation-after-retirement"), ct);
        blocked.IsFailure.ShouldBeTrue();
    }
}
