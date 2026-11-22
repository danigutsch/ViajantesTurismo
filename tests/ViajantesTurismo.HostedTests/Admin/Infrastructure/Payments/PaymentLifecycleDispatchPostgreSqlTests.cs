using PandaBear.BuildingBlocks;
using PandaBear.Idempotency;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Payments;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
[TestScope(TestScope.Integration)]
public sealed class PaymentLifecycleDispatchPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IClassFixture<PostgreSqlTestServerFixture>
{
    [Fact]
    public async Task A_dispatched_schedule_approval_commits_the_drafted_obligation()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(Currency: Currency.Eur)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await session.Seed(tour, ct);
        var draft = await session.Send(new DraftBookingPaymentScheduleCommand(booking.Id,
            new CreatePaymentScheduleDto
            {
                ExpectedVersion = 0,
                ObligationEuro = 100m,
                Installments = [new PaymentInstallmentDto { EuroAmount = 100m, DueDate = new DateOnly(2030, 1, 10) }]
            }, "owner", IdempotencyKey.From("schedule-draft")), ct);
        draft.IsSuccess.ShouldBeTrue();
        var drafted = await session.Financials.GetByBookingId(booking.Id, ct);
        drafted.ShouldNotBeNull();

        // Act
        var approval = await session.Send(new ApproveBookingPaymentScheduleCommand(booking.Id, draft.Value,
            new ApproveFinancialRecordDto
            {
                ExpectedVersion = drafted.Version,
                Confirmed = true,
                Reason = FinancialReasonDto.InitialObligation
            }, "owner", IdempotencyKey.From("schedule-approval")), ct);

        // Assert
        approval.IsSuccess.ShouldBeTrue();
        await using var reloaded = scenario.Open();
        var current = await reloaded.Financials.GetByBookingId(booking.Id, ct);
        current.ShouldNotBeNull();
        current.ApprovedObligationEuro.ShouldBe(100m);
        current.Version.ShouldBe(drafted.Version + 1);
    }

    [Fact]
    public async Task Dispatched_review_release_and_reversal_preserve_the_booking_financial_history()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        var (bookingId, receiptId, installmentId) = await scenario.SeedCustomerCredit(ct);
        await using var session = scenario.Open();
        var original = await session.Financials.GetByBookingId(bookingId, ct);
        original.ShouldNotBeNull();
        var reconciliation = original.Reconciliations.ShouldHaveSingleItem();
        var allocationRequest = new AllocatePaymentCreditDto
        {
            ExpectedVersion = original.Version,
            Confirmed = true,
            Reason = FinancialReasonDto.AllocationSelected,
            Allocations = [new InstallmentAllocationDto { TargetId = installmentId, EuroAmount = 100m }]
        };
        var allocated = await session.Send(new AllocateBookingPaymentCommand(bookingId, receiptId, allocationRequest,
            "owner", IdempotencyKey.From("credit-allocation")), ct);
        allocated.IsSuccess.ShouldBeTrue();
        var afterAllocation = await session.Financials.GetByBookingId(bookingId, ct);
        afterAllocation.ShouldNotBeNull();
        var allocation = afterAllocation.Allocations.ShouldHaveSingleItem();

        // Act
        var reviewed = await session.Send(new ReviewBookingFinancialRecordCommand(bookingId, reconciliation.Id,
            new ReviewFinancialRecordDto { ExpectedVersion = afterAllocation.Version, ChangesRequested = false },
            "reviewer", IdempotencyKey.From("reconciliation-review")), ct);
        var released = await session.Send(new ReleaseBookingPaymentAllocationsCommand(bookingId, allocationRequest with
        {
            ExpectedVersion = afterAllocation.Version + 1,
            Allocations = [new InstallmentAllocationDto { TargetId = allocation.Id, EuroAmount = 100m }]
        }, "owner", IdempotencyKey.From("credit-release")), ct);
        var reversed = await session.Send(new ReverseBookingReceiptCommand(bookingId, receiptId,
            new ApproveFinancialRecordDto { ExpectedVersion = afterAllocation.Version + 2, Confirmed = true, Reason = FinancialReasonDto.ReceiptError },
            "owner", IdempotencyKey.From("receipt-reversal")), ct);

        // Assert
        reviewed.IsSuccess.ShouldBeTrue();
        released.IsSuccess.ShouldBeTrue();
        reversed.IsSuccess.ShouldBeTrue();
        await using var restarted = scenario.Open();
        var current = await restarted.Financials.GetByBookingId(bookingId, ct);
        current.ShouldNotBeNull();
        current.Version.ShouldBe(original.Version + 4);
        current.Reviews.ShouldHaveSingleItem().TargetId.ShouldBe(reconciliation.Id);
        current.Allocations.ShouldHaveSingleItem().ActiveEuroCredit.ShouldBe(0m);
        current.Reversals.Select(item => item.Reason).ShouldBe([FinancialReasonDto.ReceiptError, FinancialReasonDto.AllocationSelected]);
        current.UnallocatedEuroCredit.ShouldBe(0m);
        current.OutstandingEuro.ShouldBe(original.OutstandingEuro);
    }

    [Fact]
    public async Task Dispatched_review_withdrawal_and_update_preserve_account_revision_history()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = await PostgreSqlPaymentScenario.Create(fixture, ct);
        await using var session = scenario.Open();
        await session.Migrate(ct);
        var now = scenario.Clock.GetUtcNow();
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX", Currency.Eur).Value;
        var account = PaymentAccount.Create("Original", recipient, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        await session.SeedAccount(account, ct);

        // Act
        var review = await session.Send(new ReviewPaymentAccountCommand(account.Id,
            new ReviewPaymentAccountDto { ExpectedVersion = account.Version, Outcome = PaymentAccountReviewOutcomeDto.Approved },
            "reviewer", IdempotencyKey.From("account-review")), ct);
        var reviewed = await session.Accounts.GetById(account.Id, ct);
        reviewed.ShouldNotBeNull();
        var withdrawal = await session.Send(new WithdrawPaymentAccountVerificationCommand(account.Id,
            new WithdrawPaymentAccountVerificationDto { ExpectedVersion = reviewed.Version, Confirmed = true },
            "owner", IdempotencyKey.From("account-withdrawal")), ct);
        var withdrawn = await session.Accounts.GetById(account.Id, ct);
        withdrawn.ShouldNotBeNull();
        var update = await session.Send(new UpdatePaymentAccountCommand(account.Id,
            new UpdatePaymentAccountDto
            {
                ExpectedVersion = withdrawn.Version,
                Label = "Replacement",
                Recipient = new PaymentRecipientDto
                {
                    Kind = PaymentAccountKindDto.Iban,
                    SettlementCurrency = "EUR",
                    RecipientName = "Recipient",
                    Iban = "DE89370400440532013000",
                    Bic = "COBADEFFXXX"
                }
            }, "owner", IdempotencyKey.From("account-update")), ct);

        // Assert
        review.IsSuccess.ShouldBeTrue();
        withdrawal.IsSuccess.ShouldBeTrue();
        update.IsSuccess.ShouldBeTrue();
        reviewed.ReviewedBy.ShouldBe("reviewer");
        reviewed.Status.ShouldBe(PaymentAccountStatusDto.Verified);
        withdrawn.Status.ShouldBe(PaymentAccountStatusDto.Draft);
        await using var reloaded = scenario.Open();
        var current = await reloaded.Accounts.GetById(account.Id, ct);
        current.ShouldNotBeNull();
        current.Label.ShouldBe("Replacement");
        current.Revision.ShouldBe(withdrawn.Revision + 1);
        current.Version.ShouldBe(account.Version + 3);
    }
}
