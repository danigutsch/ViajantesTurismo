using System.Text.Json;
using PandaBear.BuildingBlocks;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.Payments;
using ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Protects the stored currency representations that predate the shared ISO 4217 currency type.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class StoredCurrencyTests
{
    [Theory]
    [InlineData("BRL", "Real", 0)]
    [InlineData("EUR", "Euro", 1)]
    [InlineData("USD", "UsDollar", 2)]
    public void Currencies_keep_the_names_and_ordinals_that_were_stored_before_the_shared_type(string code, string name, int ordinal)
    {
        // Arrange
        var currency = Currency.Create(code).Value;

        // Act
        var storedName = StoredCurrency.ToName(currency);
        var storedOrdinal = StoredCurrency.ToOrdinal(currency);

        // Assert
        storedName.ShouldBe(name);
        storedOrdinal.ShouldBe(ordinal);
        StoredCurrency.FromName(name).ShouldBe(currency);
        StoredCurrency.FromOrdinal(ordinal).ShouldBe(currency);
    }

    [Fact]
    public void An_unknown_stored_name_is_rejected_instead_of_guessed()
    {
        // Act
        // Assert
        ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(() => StoredCurrency.FromName("BRL"));
    }

    [Fact]
    public void A_currency_without_a_stored_representation_is_rejected_when_written()
    {
        // Arrange
        var yen = Currency.Create("JPY").Value;

        // Act
        // Assert
        ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(() => StoredCurrency.ToName(yen));
        ShouldAssertionExtensions.ShouldThrow<InvalidOperationException>(() => StoredCurrency.ToOrdinal(yen));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(999)]
    public void An_undefined_stored_ordinal_becomes_a_currency_the_payment_domain_rejects(int ordinal)
    {
        // Act
        var currency = StoredCurrency.FromOrdinal(ordinal);

        // Assert
        currency.Code.ShouldBe("XXX");
    }

    [Theory]
    [InlineData("BRL", "0")]
    [InlineData("EUR", "1")]
    [InlineData("USD", "2")]
    public void Protected_payment_state_still_stores_the_currency_as_its_legacy_number(string code, string number)
    {
        // Arrange
        var options = new JsonSerializerOptions { Converters = { new StoredCurrencyJsonConverter() } };
        var currency = Currency.Create(code).Value;

        // Act
        var json = JsonSerializer.Serialize(currency, options);
        var restored = JsonSerializer.Deserialize<Currency>(number, options);

        // Assert
        json.ShouldBe(number);
        restored.ShouldBe(currency);
    }

    [Fact]
    public void A_stored_currency_that_is_not_a_number_is_rejected()
    {
        // Arrange
        var options = new JsonSerializerOptions { Converters = { new StoredCurrencyJsonConverter() } };

        // Act
        // Assert
        ShouldAssertionExtensions.ShouldThrow<JsonException>(() => JsonSerializer.Deserialize<Currency>("\"EUR\"", options));
    }

    [Fact]
    public void A_booking_financial_history_with_a_receipt_round_trips_through_the_protected_state_with_the_legacy_number()
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
        document.RootElement.GetProperty("Receipts")[0].GetProperty("Currency").GetInt32().ShouldBe(1);
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.ExportState().Receipts[0].Currency.ShouldBe(Currency.Eur);
    }
}
