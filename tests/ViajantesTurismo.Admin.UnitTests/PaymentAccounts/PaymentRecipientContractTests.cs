using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentRecipientContractTests
{
    [Theory]
    [InlineData(PixKeyTypeDto.Cpf, "52998224725")]
    [InlineData(PixKeyTypeDto.Cnpj, "11222333000181")]
    [InlineData(PixKeyTypeDto.Phone, "+5511999999999")]
    [InlineData(PixKeyTypeDto.Email, "recipient@example.test")]
    [InlineData(PixKeyTypeDto.Evp, "00000000-0000-4000-8000-000000000001")]
    public void Typed_pix_keys_validate_without_disclosing_identifiers_in_summaries(PixKeyTypeDto type, string key)
    {
        // Arrange
        var input = new PaymentRecipientDto
        {
            Kind = PaymentAccountKindDto.Pix,
            SettlementCurrency = CurrencyDto.Real,
            RecipientName = "Checked recipient",
            PixKeyType = type,
            PixKey = key
        };

        // Act
        var recipient = PaymentAccountMappings.ToRecipient(input);

        // Assert
        recipient.IsSuccess.ShouldBeTrue();
        recipient.Value.MaskedIdentifier.ShouldNotBe(key);
        recipient.Value.ToString().ShouldNotContain(key, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(BrazilianAccountTypeDto.Checking, "0001", "52998224725")]
    [InlineData(BrazilianAccountTypeDto.Salary, "1", "52998224725")]
    [InlineData(BrazilianAccountTypeDto.Savings, "9999", "11222333000181")]
    [InlineData(BrazilianAccountTypeDto.Payment, null, "11222333000181")]
    public void Brazilian_bank_inputs_preserve_the_selected_account_type_and_taxpayer_format(BrazilianAccountTypeDto type, string? branch, string taxId)
    {
        // Arrange
        var input = new PaymentRecipientDto
        {
            Kind = PaymentAccountKindDto.BrazilianBank,
            SettlementCurrency = CurrencyDto.Real,
            RecipientName = "Checked recipient",
            AccountType = type,
            Ispb = "00000000",
            Branch = branch,
            AccountNumber = "123456",
            TaxId = taxId
        };

        // Act
        var recipient = PaymentAccountMappings.ToRecipient(input);

        // Assert
        recipient.IsSuccess.ShouldBeTrue();
        recipient.Value.HolderTaxId.ShouldBe(taxId);
        recipient.Value.MaskedIdentifier.ShouldNotContain("123456", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("currency")]
    [InlineData("kind")]
    [InlineData("pix-type")]
    [InlineData("account-type")]
    [InlineData("mixed-pix")]
    [InlineData("mixed-bank")]
    [InlineData("mixed-tax")]
    [InlineData("mixed-branch")]
    [InlineData("mixed-number")]
    [InlineData("mixed-account-type")]
    [InlineData("mixed-pix-type")]
    [InlineData("undefined-rail")]
    public void Recipient_contracts_reject_undefined_labels_and_fields_from_another_rail(string field)
    {
        // Arrange
        var input = new PaymentRecipientDto
        {
            Kind = PaymentAccountKindDto.Iban,
            SettlementCurrency = CurrencyDto.Euro,
            RecipientName = "Checked recipient",
            Iban = "DE89370400440532013000",
            Bic = "COBADEFFXXX"
        };
        input = field switch
        {
            "currency" => input with { SettlementCurrency = CurrencyDto.UsDollar },
            "kind" => input with { Kind = (PaymentAccountKindDto)999 },
            "pix-type" => input with { PixKeyType = (PixKeyTypeDto)999 },
            "account-type" => input with { AccountType = (BrazilianAccountTypeDto)999 },
            "mixed-pix" => input with { PixKey = "recipient@example.test" },
            "mixed-bank" => input with { Ispb = "00000000" },
            "mixed-tax" => input with { TaxId = "52998224725" },
            "mixed-branch" => input with { Branch = "0001" },
            "mixed-number" => input with { AccountNumber = "123456" },
            "mixed-account-type" => input with { AccountType = BrazilianAccountTypeDto.Checking },
            "mixed-pix-type" => input with { PixKeyType = PixKeyTypeDto.Email },
            "undefined-rail" => input with { Kind = PaymentAccountKindDto.None },
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };

        // Act
        var recipient = PaymentAccountMappings.ToRecipient(input);

        // Assert
        recipient.IsFailure.ShouldBeTrue();
        recipient.ToString().ShouldNotContain("DE89370400440532013000", StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_or_invalid_allocations_and_installments_fail_instead_of_approving_partial_inputs()
    {
        // Act
        var missingAllocations = FinancialMappings.Allocations(null);
        var missingInstallments = FinancialMappings.Installments(null);
        var nullAllocation = FinancialMappings.Allocations([null!]);
        var nullInstallment = FinancialMappings.Installments([null!]);
        var invalidAllocation = FinancialMappings.Allocations([new InstallmentAllocationDto { TargetId = Guid.Empty, EuroAmount = 10m }]);
        var invalidInstallment = FinancialMappings.Installments([new PaymentInstallmentDto { EuroAmount = -1m, DueDate = new DateOnly(2030, 1, 5) }]);

        // Assert
        missingAllocations.IsFailure.ShouldBeTrue();
        missingInstallments.IsFailure.ShouldBeTrue();
        nullAllocation.IsFailure.ShouldBeTrue();
        nullInstallment.IsFailure.ShouldBeTrue();
        invalidAllocation.IsFailure.ShouldBeTrue();
        invalidInstallment.IsFailure.ShouldBeTrue();
    }
}
