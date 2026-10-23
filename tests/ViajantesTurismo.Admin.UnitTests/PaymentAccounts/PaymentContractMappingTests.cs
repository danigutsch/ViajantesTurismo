using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentContractMappingTests
{
    [Theory]
    [InlineData(PaymentAccountKind.None, PaymentAccountKindDto.None)]
    [InlineData(PaymentAccountKind.Iban, PaymentAccountKindDto.Iban)]
    [InlineData(PaymentAccountKind.BrazilianBank, PaymentAccountKindDto.BrazilianBank)]
    [InlineData(PaymentAccountKind.Pix, PaymentAccountKindDto.Pix)]
    public void Payment_account_kind_labels_preserve_the_contract_and_domain_meaning(PaymentAccountKind domain, PaymentAccountKindDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_payment_account_kind_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((PaymentAccountKindDto)value);
        Action project = () => PaymentAccountMappings.Map((PaymentAccountKind)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PaymentAccountStatus.Draft, PaymentAccountStatusDto.Draft)]
    [InlineData(PaymentAccountStatus.Verified, PaymentAccountStatusDto.Verified)]
    [InlineData(PaymentAccountStatus.Inactive, PaymentAccountStatusDto.Inactive)]
    public void Payment_account_status_labels_preserve_the_contract_and_domain_meaning(PaymentAccountStatus domain, PaymentAccountStatusDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_payment_account_status_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((PaymentAccountStatusDto)value);
        Action project = () => PaymentAccountMappings.Map((PaymentAccountStatus)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(BrazilianAccountType.None, BrazilianAccountTypeDto.None)]
    [InlineData(BrazilianAccountType.Checking, BrazilianAccountTypeDto.Checking)]
    [InlineData(BrazilianAccountType.Salary, BrazilianAccountTypeDto.Salary)]
    [InlineData(BrazilianAccountType.Savings, BrazilianAccountTypeDto.Savings)]
    [InlineData(BrazilianAccountType.Payment, BrazilianAccountTypeDto.Payment)]
    public void Brazilian_account_type_labels_preserve_the_contract_and_domain_meaning(BrazilianAccountType domain, BrazilianAccountTypeDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_brazilian_account_type_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((BrazilianAccountTypeDto)value);
        Action project = () => PaymentAccountMappings.Map((BrazilianAccountType)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PaymentAccountVerificationMethod.None, PaymentAccountVerificationMethodDto.None)]
    [InlineData(PaymentAccountVerificationMethod.BankStatement, PaymentAccountVerificationMethodDto.BankStatement)]
    [InlineData(PaymentAccountVerificationMethod.BankConfirmation, PaymentAccountVerificationMethodDto.BankConfirmation)]
    [InlineData(PaymentAccountVerificationMethod.RecipientConfirmation, PaymentAccountVerificationMethodDto.RecipientConfirmation)]
    public void Payment_account_verification_method_labels_preserve_the_contract_and_domain_meaning(PaymentAccountVerificationMethod domain, PaymentAccountVerificationMethodDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_payment_account_verification_method_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((PaymentAccountVerificationMethodDto)value);
        Action project = () => PaymentAccountMappings.Map((PaymentAccountVerificationMethod)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PaymentAccountReviewOutcome.None, PaymentAccountReviewOutcomeDto.None)]
    [InlineData(PaymentAccountReviewOutcome.Approved, PaymentAccountReviewOutcomeDto.Approved)]
    [InlineData(PaymentAccountReviewOutcome.ChangesRequested, PaymentAccountReviewOutcomeDto.ChangesRequested)]
    public void Payment_account_review_outcome_labels_preserve_the_contract_and_domain_meaning(PaymentAccountReviewOutcome domain, PaymentAccountReviewOutcomeDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_payment_account_review_outcome_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((PaymentAccountReviewOutcomeDto)value);
        Action project = () => PaymentAccountMappings.Map((PaymentAccountReviewOutcome)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PaymentAccountReason.None, PaymentAccountReasonDto.None)]
    [InlineData(PaymentAccountReason.DraftCreated, PaymentAccountReasonDto.DraftCreated)]
    [InlineData(PaymentAccountReason.RecipientChanged, PaymentAccountReasonDto.RecipientChanged)]
    [InlineData(PaymentAccountReason.BankCheckConfirmed, PaymentAccountReasonDto.BankCheckConfirmed)]
    [InlineData(PaymentAccountReason.ReviewAccepted, PaymentAccountReasonDto.ReviewAccepted)]
    [InlineData(PaymentAccountReason.ReviewChangesRequested, PaymentAccountReasonDto.ReviewChangesRequested)]
    [InlineData(PaymentAccountReason.ApprovalWithdrawn, PaymentAccountReasonDto.ApprovalWithdrawn)]
    [InlineData(PaymentAccountReason.NoLongerUsed, PaymentAccountReasonDto.NoLongerUsed)]
    [InlineData(PaymentAccountReason.AccountReplaced, PaymentAccountReasonDto.AccountReplaced)]
    [InlineData(PaymentAccountReason.SensitiveAccess, PaymentAccountReasonDto.SensitiveAccess)]
    public void Payment_account_reason_labels_preserve_the_contract_and_domain_meaning(PaymentAccountReason domain, PaymentAccountReasonDto contract)
    {
        // Act
        var projected = PaymentAccountMappings.Map(domain);
        var parsed = PaymentAccountMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_payment_account_reason_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => PaymentAccountMappings.Map((PaymentAccountReasonDto)value);
        Action project = () => PaymentAccountMappings.Map((PaymentAccountReason)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(FinancialReason.None, FinancialReasonDto.None)]
    [InlineData(FinancialReason.InitialObligation, FinancialReasonDto.InitialObligation)]
    [InlineData(FinancialReason.TermsChanged, FinancialReasonDto.TermsChanged)]
    [InlineData(FinancialReason.CancellationAdjustment, FinancialReasonDto.CancellationAdjustment)]
    [InlineData(FinancialReason.QuoteAgreed, FinancialReasonDto.QuoteAgreed)]
    [InlineData(FinancialReason.ExactReceipt, FinancialReasonDto.ExactReceipt)]
    [InlineData(FinancialReason.LateReceipt, FinancialReasonDto.LateReceipt)]
    [InlineData(FinancialReason.ShortReceipt, FinancialReasonDto.ShortReceipt)]
    [InlineData(FinancialReason.ExcessReceipt, FinancialReasonDto.ExcessReceipt)]
    [InlineData(FinancialReason.FeeDifference, FinancialReasonDto.FeeDifference)]
    [InlineData(FinancialReason.MismatchedReceipt, FinancialReasonDto.MismatchedReceipt)]
    [InlineData(FinancialReason.AllocationSelected, FinancialReasonDto.AllocationSelected)]
    [InlineData(FinancialReason.ReceiptError, FinancialReasonDto.ReceiptError)]
    [InlineData(FinancialReason.RefundPerformed, FinancialReasonDto.RefundPerformed)]
    [InlineData(FinancialReason.RefundRecordError, FinancialReasonDto.RefundRecordError)]
    [InlineData(FinancialReason.ApprovalWithdrawn, FinancialReasonDto.ApprovalWithdrawn)]
    [InlineData(FinancialReason.DraftSaved, FinancialReasonDto.DraftSaved)]
    [InlineData(FinancialReason.ReceiptRecorded, FinancialReasonDto.ReceiptRecorded)]
    [InlineData(FinancialReason.ReviewAccepted, FinancialReasonDto.ReviewAccepted)]
    [InlineData(FinancialReason.ReviewChangesRequested, FinancialReasonDto.ReviewChangesRequested)]
    [InlineData(FinancialReason.InstructionGenerated, FinancialReasonDto.InstructionGenerated)]
    [InlineData(FinancialReason.InstructionApproved, FinancialReasonDto.InstructionApproved)]
    [InlineData(FinancialReason.InstructionFinalized, FinancialReasonDto.InstructionFinalized)]
    [InlineData(FinancialReason.InstructionDownloaded, FinancialReasonDto.InstructionDownloaded)]
    [InlineData(FinancialReason.InstructionVoided, FinancialReasonDto.InstructionVoided)]
    public void Financial_reason_labels_preserve_the_contract_and_domain_meaning(FinancialReason domain, FinancialReasonDto contract)
    {
        // Act
        var projected = FinancialMappings.Map(domain);
        var parsed = FinancialMappings.Map(contract);

        // Assert
        projected.ShouldBe(contract);
        parsed.ShouldBe(domain);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void Undefined_financial_reason_labels_are_rejected_in_both_directions(int value)
    {
        // Act
        Action parse = () => FinancialMappings.Map((FinancialReasonDto)value);
        Action project = () => FinancialMappings.Map((FinancialReason)value);

        // Assert
        parse.ShouldThrow<ArgumentOutOfRangeException>();
        project.ShouldThrow<ArgumentOutOfRangeException>();
    }

}
