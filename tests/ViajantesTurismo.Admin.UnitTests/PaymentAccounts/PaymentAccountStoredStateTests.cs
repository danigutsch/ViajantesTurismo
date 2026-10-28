using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentAccountStoredStateTests
{
    [Theory]
    [InlineData("unchanged", true)]
    [InlineData("schema", false)]
    [InlineData("id", false)]
    [InlineData("version", false)]
    [InlineData("label", false)]
    [InlineData("status", false)]
    [InlineData("revisions", false)]
    [InlineData("reviews", false)]
    [InlineData("missing-version-events", false)]
    [InlineData("revision-id", false)]
    [InlineData("duplicate-revision", false)]
    [InlineData("revision-order", false)]
    [InlineData("creator", false)]
    [InlineData("created-at", false)]
    [InlineData("backdated-revision", false)]
    [InlineData("verifier", false)]
    [InlineData("verified-at", false)]
    [InlineData("verified-default", false)]
    [InlineData("verified-before-create", false)]
    [InlineData("check-missing", false)]
    [InlineData("check-default", false)]
    [InlineData("check-future", false)]
    [InlineData("check-method", false)]
    [InlineData("check-method-undefined", false)]
    [InlineData("orphan-attestation", false)]
    [InlineData("draft-verified", false)]
    [InlineData("verified-unattested", false)]
    [InlineData("review-revision", false)]
    [InlineData("review-future-revision", false)]
    [InlineData("review-actor", false)]
    [InlineData("review-date", false)]
    [InlineData("review-before-creation", false)]
    [InlineData("review-creator", false)]
    [InlineData("review-outcome", false)]
    [InlineData("recipient-currency", false)]
    public void Stored_account_revisions_require_consistent_verification_and_independent_reviews(string change, bool valid)
    {
        // Arrange
        var now = PaymentScenario.Now;
        var recipient = PaymentRecipientDetails.CreateIban("Checked recipient", "DE89370400440532013000", "COBADEFFXXX").Value;
        var account = PaymentAccount.Create("Checked account", recipient, "owner", now).Value;
        account.Verify(account.Version, "owner", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        account.Review(account.Version, "reviewer", now, PaymentAccountReviewOutcome.ChangesRequested).IsSuccess.ShouldBeTrue();
        account.Revise(account.Version, "Replacement", recipient, "owner", now).IsSuccess.ShouldBeTrue();
        account.Verify(account.Version, "other-admin", now, PaymentAccountVerificationMethod.RecipientConfirmation, now, true, true).IsSuccess.ShouldBeTrue();
        var state = account.ExportState();
        var first = state.Revisions[0];
        var second = state.Revisions[1];
        var stored = change switch
        {
            "unchanged" => state,
            "schema" => state with { SchemaVersion = 2 },
            "id" => state with { Id = Guid.Empty },
            "version" => state with { Version = 0 },
            "label" => state with { Label = " invalid " },
            "status" => state with { Status = (PaymentAccountStatus)999 },
            "revisions" => state with { Revisions = [] },
            "reviews" => state with { Reviews = null! },
            "missing-version-events" => state with { Version = 1 },
            "revision-id" => state with { Revisions = [first with { Id = Guid.Empty }, second] },
            "duplicate-revision" => state with { Revisions = [first, second with { Id = first.Id }] },
            "revision-order" => state with { Revisions = [first with { Number = 2 }, second] },
            "creator" => state with { Revisions = [first with { CreatedBy = "" }, second] },
            "created-at" => state with { Revisions = [first with { CreatedAt = default }, second] },
            "backdated-revision" => state with { Revisions = [first, second with { CreatedAt = first.CreatedAt.AddSeconds(-1) }] },
            "verifier" => state with { Revisions = [first with { VerifiedBy = "" }, second] },
            "verified-at" => state with { Revisions = [first with { VerifiedAt = null }, second] },
            "verified-default" => state with { Revisions = [first with { VerifiedAt = default(DateTimeOffset) }, second] },
            "verified-before-create" => state with { Revisions = [first with { VerifiedAt = first.CreatedAt.AddSeconds(-1) }, second] },
            "check-missing" => state with { Revisions = [first with { CheckedAt = null }, second] },
            "check-default" => state with { Revisions = [first with { CheckedAt = default(DateTimeOffset) }, second] },
            "check-future" => state with { Revisions = [first with { CheckedAt = first.VerifiedAt.ShouldNotBeNull().AddHours(1) }, second] },
            "check-method" => state with { Revisions = [first with { Method = PaymentAccountVerificationMethod.None }, second] },
            "check-method-undefined" => state with { Revisions = [first with { Method = (PaymentAccountVerificationMethod)999 }, second] },
            "orphan-attestation" => state with { Revisions = [first with { VerifiedBy = null }, second] },
            "draft-verified" => state with { Status = PaymentAccountStatus.Draft },
            "verified-unattested" => state with { Revisions = [first, second with { VerifiedBy = null, VerifiedAt = null, CheckedAt = null, Method = PaymentAccountVerificationMethod.None }] },
            "review-revision" => state with { Reviews = [state.Reviews[0] with { Revision = 0 }] },
            "review-future-revision" => state with { Reviews = [state.Reviews[0] with { Revision = 3 }] },
            "review-actor" => state with { Reviews = [state.Reviews[0] with { ReviewerId = "" }] },
            "review-date" => state with { Reviews = [state.Reviews[0] with { ReviewedAt = default }] },
            "review-before-creation" => state with { Reviews = [state.Reviews[0] with { ReviewedAt = first.CreatedAt.AddSeconds(-1) }] },
            "review-creator" => state with { Reviews = [state.Reviews[0] with { ReviewerId = first.CreatedBy }] },
            "review-outcome" => state with { Reviews = [state.Reviews[0] with { Outcome = PaymentAccountReviewOutcome.None }] },
            "recipient-currency" => state with { Revisions = [first with { Recipient = first.Recipient with { SettlementCurrency = Admin.Domain.Tours.Currency.UsDollar } }, second] },
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };

        // Act
        var restored = PaymentAccount.Restore(stored);

        // Assert
        restored.IsSuccess.ShouldBe(valid);
        if (valid)
        {
            restored.Value.CanDeliver(second.Id).ShouldBeTrue();
            restored.Value.CanDeliver(first.Id).ShouldBeFalse();
            restored.Value.RequiresAttention.ShouldBeFalse();
            restored.Value.Current.IsSelfApproved.ShouldBeFalse();
        }
    }
}
