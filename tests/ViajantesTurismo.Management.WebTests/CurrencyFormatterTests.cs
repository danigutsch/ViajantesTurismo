using PandaBear.Testing;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Protects the presentation of ISO 4217 currency codes.</summary>
[TestScope(TestScope.Unit)]
public sealed class CurrencyFormatterTests
{
    [Fact]
    public void Every_selectable_currency_has_a_symbol_and_a_display_name()
    {
        // Arrange
        string[] codes = [.. CurrencyFormatter.TourCurrencies, .. CurrencyFormatter.SettlementCurrencies];

        // Act
        var symbols = codes.Select(CurrencyFormatter.GetSymbol).ToArray();
        var names = codes.Select(CurrencyFormatter.GetDisplayName).ToArray();

        // Assert
        symbols.ShouldNotContain("¤");
        names.ShouldNotContain(name => codes.Contains(name));
    }

    [Theory]
    [InlineData("BRL", 1234.5, "R$ 1,234.50")]
    [InlineData("USD", 10, "$ 10.00")]
    [InlineData("EUR", 1234.5, "1,234.50 €")]
    public void Places_the_symbol_by_currency_code(string code, double amount, string expected)
    {
        // Act
        var formatted = CurrencyFormatter.Format((decimal)amount, code);

        // Assert
        formatted.ShouldBe(expected);
    }

    [Fact]
    public void A_code_without_a_presentation_falls_back_to_the_generic_sign_and_the_code_itself()
    {
        // Act
        var symbol = CurrencyFormatter.GetSymbol("JPY");
        var name = CurrencyFormatter.GetDisplayName("JPY");

        // Assert
        symbol.ShouldBe("¤");
        name.ShouldBe("JPY");
    }
}
