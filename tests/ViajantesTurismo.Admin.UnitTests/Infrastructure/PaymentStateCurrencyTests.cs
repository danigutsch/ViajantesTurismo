using System.Text.Json;
using PandaBear.BuildingBlocks;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Protects the ISO 4217 code that protected payment state stores for every currency.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentStateCurrencyTests
{
    [Fact]
    public void A_booking_financial_history_stores_receipt_currency_as_its_iso_code_and_round_trips()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        scenario.RecordReceipt(50m, 0m, installment);

        // Act
        var json = JsonSerializer.Serialize(scenario.History.ExportState(), PaymentPersistenceJsonContext.Default.BookingFinancialState);
        var state = JsonSerializer.Deserialize(json, PaymentPersistenceJsonContext.Default.BookingFinancialState);
        var restored = BookingFinancialHistory.Restore(state);

        // Assert
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("Receipts")[0].GetProperty("Currency").GetString().ShouldBe("EUR");
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ExportState().Receipts[0].Currency.ShouldBe(Currency.Eur);
    }

    [Theory]
    [InlineData("BRL")]
    [InlineData("EUR")]
    public void A_payment_account_stores_its_settlement_currency_as_the_iso_code_and_round_trips(string code)
    {
        // Arrange
        var currency = Currency.Create(code).Value;
        var recipient = PaymentRecipientDetails.CreateIban("Recipient", "DE89370400440532013000", "COBADEFFXXX", currency).Value;
        var account = PaymentAccount.Create("Account", recipient, "owner", PaymentScenario.Now).Value;

        // Act
        var json = JsonSerializer.Serialize(account.ExportState(), PaymentPersistenceJsonContext.Default.PaymentAccountState);
        var state = JsonSerializer.Deserialize(json, PaymentPersistenceJsonContext.Default.PaymentAccountState);
        var restored = PaymentAccount.Restore(state);

        // Assert
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("Revisions")[0].GetProperty("Recipient").GetProperty("SettlementCurrency").GetString().ShouldBe(code);
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.Current.Recipient.SettlementCurrency.ShouldBe(currency);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("{}")]
    public void A_stored_currency_that_is_not_a_code_string_is_rejected(string token)
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        scenario.RecordReceipt(50m, 0m, installment);
        var json = JsonSerializer.Serialize(scenario.History.ExportState(), PaymentPersistenceJsonContext.Default.BookingFinancialState);
        var tampered = json.Replace("\"Currency\":\"EUR\"", "\"Currency\":" + token, StringComparison.Ordinal);

        // Act

        // Assert
        tampered.ShouldNotBe(json);
        ShouldAssertionExtensions.ShouldThrow<JsonException>(() => JsonSerializer.Deserialize(tampered, PaymentPersistenceJsonContext.Default.BookingFinancialState));
    }

    [Fact]
    public void A_stored_currency_the_payment_domain_does_not_support_is_rejected_on_restore()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        scenario.RecordReceipt(50m, 0m, installment);
        var json = JsonSerializer.Serialize(scenario.History.ExportState(), PaymentPersistenceJsonContext.Default.BookingFinancialState);
        var tampered = json.Replace("\"Currency\":\"EUR\"", "\"Currency\":\"USD\"", StringComparison.Ordinal);

        // Act
        var state = JsonSerializer.Deserialize(tampered, PaymentPersistenceJsonContext.Default.BookingFinancialState);
        var restored = BookingFinancialHistory.Restore(state);

        // Assert
        tampered.ShouldNotBe(json);
        restored.IsFailure.ShouldBeTrue();
    }
}
