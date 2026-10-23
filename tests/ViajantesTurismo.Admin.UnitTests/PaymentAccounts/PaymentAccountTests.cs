using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using PandaBear.BuildingBlocks;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentAccountTests
{
    [Fact]
    public void Solo_admin_can_draft_verify_withdraw_and_retire_without_another_user()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value;
        var account = PaymentAccount.Create("EUR account", recipient, "admin", now).Value;
        var revision = account.Current.Id;

        // Act / Assert: saving never approves; verification is explicit and self-approval stays visible.
        account.Status.ShouldBe(PaymentAccountStatus.Draft);
        account.CanDeliver(revision).ShouldBeFalse();
        account.Verify(account.Version, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, true, false).IsFailure.ShouldBeTrue();
        account.Verify(account.Version, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        account.Current.IsSelfApproved.ShouldBeTrue();
        account.CanDeliver(revision).ShouldBeTrue();
        account.WithdrawVerification(account.Version, "admin", now, true).IsSuccess.ShouldBeTrue();
        account.CanDeliver(revision).ShouldBeFalse();
        account.Revisions[0].VerifiedBy.ShouldBe("admin");
        account.Retire(account.Version, "admin", now, PaymentAccountReason.NoLongerUsed, true).IsSuccess.ShouldBeTrue();
        account.Status.ShouldBe(PaymentAccountStatus.Inactive);
    }

    [Fact]
    public void Second_admin_can_complete_a_draft_and_changes_requested_never_silently_revoke_verification()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreatePix("Recipient", PixKeyType.Email, "payments@example.com").Value;
        var account = PaymentAccount.Create("Pix account", recipient, "first", now).Value;

        // Act
        var verified = account.Verify(account.Version, "second", now, PaymentAccountVerificationMethod.RecipientConfirmation, now, true, true);
        var reviewed = account.Review(account.Version, "second", now, PaymentAccountReviewOutcome.ChangesRequested);

        // Assert
        verified.IsSuccess.ShouldBeTrue();
        reviewed.IsSuccess.ShouldBeTrue();
        account.Current.IsSelfApproved.ShouldBeFalse();
        account.Status.ShouldBe(PaymentAccountStatus.Verified);
        account.RequiresAttention.ShouldBeTrue();
        account.ReviewedBy.ShouldBe("second");
        account.CanDeliver(account.Current.Id).ShouldBeTrue();
    }

    [Fact]
    public void Recipient_changes_create_an_unverified_revision_and_invalidate_its_predecessors_review()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreatePix("Recipient", PixKeyType.Email, "payments@example.com").Value;
        var account = PaymentAccount.Create("Pix account", recipient, "first", now).Value;
        account.Verify(account.Version, "first", now, PaymentAccountVerificationMethod.BankConfirmation, now, true, true).IsSuccess.ShouldBeTrue();
        account.Review(account.Version, "second", now, PaymentAccountReviewOutcome.Approved).IsSuccess.ShouldBeTrue();
        var previous = account.Current.Id;

        // Act
        var result = account.Revise(account.Version, "Replacement", recipient, "second", now);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        account.Current.Number.ShouldBe(2);
        account.Status.ShouldBe(PaymentAccountStatus.Draft);
        account.ReviewOutcome.ShouldBe(PaymentAccountReviewOutcome.None);
        account.CanDeliver(previous).ShouldBeFalse();
        account.Revisions[0].VerifiedBy.ShouldBe("first");
    }

    [Fact]
    public void Creator_cannot_supply_an_independent_review_and_stale_edits_conflict()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreatePix("Recipient", PixKeyType.Email, "payments@example.com").Value;
        var account = PaymentAccount.Create("Pix account", recipient, "admin", now).Value;
        var originalVersion = account.Version;

        // Act
        var review = account.Review(originalVersion, "admin", now, PaymentAccountReviewOutcome.Approved);
        account.Verify(originalVersion, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var edit = account.Revise(originalVersion, "Changed", recipient, "admin", now);

        // Assert
        review.IsFailure.ShouldBeTrue();
        edit.Status.ShouldBe(ResultStatus.Conflict);
        account.Current.Number.ShouldBe(1);
        account.Status.ShouldBe(PaymentAccountStatus.Verified);
    }

    [Fact]
    public void Retiring_a_verified_account_blocks_existing_instruction_delivery_and_all_further_mutations()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value;
        var account = PaymentAccount.Create("EUR account", recipient, "admin", now).Value;
        account.Verify(account.Version, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var revision = account.Current.Id;

        // Act
        var retired = account.Retire(account.Version, "admin", now, PaymentAccountReason.AccountReplaced, true);
        var edit = account.Revise(account.Version, "New", recipient, "other", now);

        // Assert
        retired.IsSuccess.ShouldBeTrue();
        account.CanDeliver(revision).ShouldBeFalse();
        edit.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Restoring_state_preserves_history_but_rejects_corrupt_verification_and_review()
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value;
        var account = PaymentAccount.Create("EUR account", recipient, "admin", now).Value;
        account.Verify(account.Version, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, true, true).IsSuccess.ShouldBeTrue();
        var state = account.ExportState();

        // Act
        var restored = PaymentAccount.Restore(state);
        var corruptVerification = PaymentAccount.Restore(state with
        {
            Revisions = [state.Revisions[0] with { VerifiedAt = null }]
        });
        var corruptReview = PaymentAccount.Restore(state with
        {
            Reviews = [new PaymentAccountReviewState(1, "admin", now, PaymentAccountReviewOutcome.Approved)],
            Version = state.Version + 1
        });

        // Assert
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.Current.IsSelfApproved.ShouldBeTrue();
        restored.Value.GetDomainEvents().Count.ShouldBe(0);
        corruptVerification.IsFailure.ShouldBeTrue();
        corruptReview.IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Verification_requires_both_explicit_confirmation_and_independent_check_attestation(bool attested, bool confirmed)
    {
        // Arrange
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var recipient = PaymentRecipientDetails.CreatePix("Recipient", PixKeyType.Email, "payments@example.com").Value;
        var account = PaymentAccount.Create("Pix account", recipient, "admin", now).Value;

        // Act
        var result = account.Verify(account.Version, "admin", now, PaymentAccountVerificationMethod.BankStatement, now, attested, confirmed);

        // Assert
        result.IsFailure.ShouldBeTrue();
        account.Status.ShouldBe(PaymentAccountStatus.Draft);
        account.Version.ShouldBe(1);
    }

    [Fact]
    public void Ordinary_diagnostics_mask_identifiers_and_invalid_taxpayer_checksums_fail()
    {
        // Arrange
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX").Value;

        // Act
        var invalid = PaymentRecipientDetails.CreateBrazilian("Recipient", "12345678", "1234", "123456", BrazilianAccountType.Checking, "11122233300");
        var valid = PaymentRecipientDetails.CreateBrazilian("Recipient", "12345678", "1234", "123456", BrazilianAccountType.Checking, "11122233396");

        // Assert
        recipient.MaskedIdentifier.ShouldBe("…3000");
        recipient.ToString().ShouldNotContain("DE89", StringComparison.Ordinal);
        valid.IsSuccess.ShouldBeTrue();
        invalid.IsFailure.ShouldBeTrue();
    }
}
