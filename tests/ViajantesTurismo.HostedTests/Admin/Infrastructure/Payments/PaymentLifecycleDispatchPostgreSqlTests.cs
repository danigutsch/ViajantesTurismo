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
