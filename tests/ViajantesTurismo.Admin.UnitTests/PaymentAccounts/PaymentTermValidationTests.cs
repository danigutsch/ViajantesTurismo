using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentTermValidationTests
{
    [Theory]
    [InlineData("number")]
    [InlineData("negative")]
    [InlineData("precision")]
    [InlineData("price")]
    [InlineData("actor")]
    [InlineData("time")]
    [InlineData("zone")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("duplicate")]
    [InlineData("unordered")]
    [InlineData("excess")]
    [InlineData("short")]
    public void Schedule_terms_reject_invalid_totals_order_precision_and_creation_metadata(string invalid)
    {
        // Arrange
        var first = PaymentInstallment.Create(50m, PaymentScenario.DueDate).Value;
        var second = PaymentInstallment.Create(50m, PaymentScenario.DueDate.AddDays(1)).Value;
        IReadOnlyList<PaymentInstallment> installments = invalid switch
        {
            "missing" => [],
            "null" => [null!],
            "duplicate" => [first, first],
            "unordered" => [second, first],
            "excess" => [first, second, first],
            "short" => [first],
            _ => [first, second]
        };
        var now = invalid == "time" ? default : invalid == "zone" ? PaymentScenario.Now.ToOffset(TimeSpan.FromHours(1)) : PaymentScenario.Now;

        // Act
        var result = PaymentScheduleRevision.Create(invalid == "number" ? 0 : 1,
            invalid == "negative" ? -100m : invalid == "precision" ? 100.001m : 100m,
            invalid == "price" ? 0m : 100m, installments, invalid == "actor" ? "" : "owner", now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("rate")]
    [InlineData("precision")]
    [InlineData("source")]
    [InlineData("source-padding")]
    [InlineData("source-length")]
    [InlineData("source-control")]
    [InlineData("reference")]
    [InlineData("agreement")]
    [InlineData("agreement-zone")]
    [InlineData("expiry-zone")]
    [InlineData("clock-zone")]
    [InlineData("future")]
    [InlineData("expiry")]
    public void Agreed_quotes_reject_missing_evidence_and_ambiguous_or_invalid_instants(string invalid)
    {
        // Arrange
        var now = PaymentScenario.Now;
        var source = invalid switch
        {
            "source" => "",
            "source-padding" => " bank ",
            "source-length" => new string('a', 129),
            "source-control" => "bank\n",
            _ => "bank"
        };

        // Act
        var result = AgreedFxQuote.Create(invalid == "rate" ? 0m : invalid == "precision" ? 5.000000001m : 5m,
            source, invalid == "reference" ? "" : "agreement",
            invalid == "agreement" ? default : invalid == "future" ? now.AddSeconds(1) : invalid == "agreement-zone" ? now.ToOffset(TimeSpan.FromHours(1)) : now,
            invalid == "expiry" ? now : invalid == "expiry-zone" ? now.AddHours(1).ToOffset(TimeSpan.FromHours(1)) : now.AddHours(1),
            invalid == "clock-zone" ? now.ToOffset(TimeSpan.FromHours(1)) : now);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Settlement_rejects_overflow_or_fractional_euro_inputs_and_expiry_is_exclusive()
    {
        // Arrange
        var now = PaymentScenario.Now;
        var quote = AgreedFxQuote.Create(5m, "bank", "agreement", now, now.AddHours(1), now).Value;

        // Act
        var overflow = quote.Settle(decimal.MaxValue);
        var fractional = quote.Settle(1.001m);
        var negative = quote.Settle(-1m);
        var rounded = quote.Settle(0.01m);

        // Assert
        overflow.IsFailure.ShouldBeTrue();
        fractional.IsFailure.ShouldBeTrue();
        negative.IsFailure.ShouldBeTrue();
        rounded.Value.Amount.ShouldBe(0.05m);
        quote.IsTimely(now.AddSeconds(-1)).ShouldBeFalse();
        quote.IsTimely(now).ShouldBeTrue();
        quote.IsTimely(now.AddHours(1)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("unconfirmed")]
    [InlineData("actor")]
    [InlineData("actor-length")]
    [InlineData("actor-control")]
    [InlineData("time")]
    [InlineData("zone")]
    [InlineData("reason")]
    [InlineData("undefined-reason")]
    public void Approval_requires_an_explicit_confirmation_identified_actor_utc_instant_and_bounded_reason(string invalid)
    {
        // Arrange
        var actor = invalid switch
        {
            "actor" => "",
            "actor-length" => new string('a', 257),
            "actor-control" => "owner\n",
            _ => "owner"
        };
        var now = invalid == "time" ? default : invalid == "zone" ? PaymentScenario.Now.ToOffset(TimeSpan.FromHours(1)) : PaymentScenario.Now;
        var reason = invalid == "reason" ? FinancialReason.None : invalid == "undefined-reason" ? (FinancialReason)999 : FinancialReason.TermsChanged;

        // Act
        var result = FinancialApproval.Create(actor, now, reason, invalid != "unconfirmed");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("missing-installments")]
    [InlineData("installment-id")]
    [InlineData("installment-amount")]
    [InlineData("approval-actor")]
    [InlineData("approval-time")]
    public void Stored_schedules_reject_broken_installments_and_backdated_approval(string invalid)
    {
        // Arrange
        var installment = PaymentInstallment.Create(100m, PaymentScenario.DueDate).Value;
        var draft = PaymentScheduleRevision.Create(1, 100m, 100m, [installment], "owner", PaymentScenario.Now).Value;
        var approved = draft.Approve(FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.InitialObligation, true).Value).ExportState();
        var state = invalid switch
        {
            "id" => approved with { Id = Guid.Empty },
            "missing-installments" => approved with { Installments = null! },
            "installment-id" => approved with { Installments = [approved.Installments[0] with { Id = Guid.Empty }] },
            "installment-amount" => approved with { Installments = [approved.Installments[0] with { Amount = 0m }] },
            "approval-actor" => approved with { Approval = approved.Approval.ShouldNotBeNull() with { ActorId = "" } },
            "approval-time" => approved with { Approval = approved.Approval.ShouldNotBeNull() with { ApprovedAt = PaymentScenario.Now.AddSeconds(-1) } },
            _ => throw new ArgumentOutOfRangeException(nameof(invalid))
        };

        // Act
        var restored = PaymentScheduleRevision.Restore(state);

        // Assert
        restored.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Financial_value_equality_includes_currency_fees_quote_evidence_and_approval_identity()
    {
        // Arrange
        var now = PaymentScenario.Now;
        var euro = PaymentMoney.Create(100m, Admin.Domain.Tours.Currency.Euro).Value;
        var real = PaymentMoney.Create(100m, Admin.Domain.Tours.Currency.Real).Value;
        var quote = AgreedFxQuote.Create(5m, "bank", "agreement", now, now.AddHours(1), now).Value;
        var approval = FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value;
        var receipt = ReceiptSettlement.Create(Admin.Domain.Tours.Currency.Euro, 100m, 1m).Value;

        // Act
        var sameMoney = PaymentMoney.Create(100m, Admin.Domain.Tours.Currency.Euro).Value;
        var sameQuote = AgreedFxQuote.Create(5m, "bank", "agreement", now, now.AddHours(1), now).Value;
        var sameApproval = FinancialApproval.Create("owner", now, FinancialReason.InitialObligation, true).Value;
        var sameReceipt = ReceiptSettlement.Create(Admin.Domain.Tours.Currency.Euro, 100m, 1m).Value;

        // Assert
        euro.ShouldBe(sameMoney);
        euro.ShouldNotBe(real);
        quote.ShouldBe(sameQuote);
        approval.ShouldBe(sameApproval);
        receipt.ShouldBe(sameReceipt);
        receipt.NetAmount.ShouldBe(99m);
    }
}
