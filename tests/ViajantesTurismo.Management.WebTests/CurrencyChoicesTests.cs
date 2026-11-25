using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Protects the currencies the forms offer.</summary>
[TestScope(TestScope.Unit)]
public sealed class CurrencyChoicesTests
{
    [Fact]
    public void Every_selectable_currency_has_a_display_name()
    {
        // Arrange
        string[] codes = [.. CurrencyChoices.Tour.Select(currency => currency.Code), .. CurrencyChoices.Settlement.Select(currency => currency.Code)];

        // Act
        var names = codes.Select(CurrencyChoices.DisplayName).ToArray();

        // Assert
        names.ShouldNotContain(name => codes.Contains(name));
    }

    [Fact]
    public void A_code_without_a_display_name_falls_back_to_the_code_itself()
    {
        // Act
        var name = CurrencyChoices.DisplayName("JPY");

        // Assert
        name.ShouldBe("JPY");
    }
}
