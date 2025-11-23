using ViajantesTurismo.Admin.Web.Components.Shared;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.WebTests;

public class CountrySelectorTests : BunitContext
{
    [Fact]
    public void Renders_With_Default_Placeholder_When_No_Value_Selected()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" },
            new() { Code = "FR", Name = "France" }
        };
        var value = string.Empty;

        // Act
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var button = cut.Find("button.form-select");
        button.MarkupMatches("""
                             <button type="button" class="form-select text-start d-flex align-items-center">
                                 <span class="text-muted">-- Select Country --</span>
                             </button>
                             """);
    }

    [Fact]
    public void Renders_Selected_Country_With_Flag_And_Name()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" },
            new() { Code = "FR", Name = "France" }
        };
        var value = "Portugal";

        // Act
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var button = cut.Find("button.form-select");
        button.MarkupMatches("""
                             <button type="button" class="form-select text-start d-flex align-items-center">
                                 <span class="fi fi-PT me-2"></span>
                                 <span>Portugal</span>
                             </button>
                             """);
    }

    [Fact]
    public void Dropdown_Is_Initially_Closed()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" }
        };
        var value = string.Empty;

        // Act
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var dropdowns = cut.FindAll(".country-dropdown-menu");
        Assert.Empty(dropdowns);
    }

    [Fact]
    public void Clicking_Button_Opens_Dropdown()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert
        var dropdown = cut.Find(".country-dropdown-menu");
        Assert.NotNull(dropdown);
    }

    [Fact]
    public void Dropdown_Shows_All_Countries_Initially()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" },
            new() { Code = "FR", Name = "France" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Equal(3, items.Count);
        Assert.Contains(items, i => i.TextContent.Contains("Portugal"));
        Assert.Contains(items, i => i.TextContent.Contains("Spain"));
        Assert.Contains(items, i => i.TextContent.Contains("France"));
    }

    [Fact]
    public void Search_Input_Filters_Countries()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" },
            new() { Code = "FR", Name = "France" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Type in search
        var searchInput = cut.Find("input[type='text']");
        cut.InvokeAsync(() => searchInput.Input("Por"));
        cut.WaitForState(() => cut.FindAll(".country-dropdown-item").Count == 1);

        // Assert
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Single(items);
        Assert.Contains("Portugal", items[0].TextContent);
    }

    [Fact]
    public void Search_Is_Case_Insensitive()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Type in search (lowercase)
        var searchInput = cut.Find("input[type='text']");
        cut.InvokeAsync(() => searchInput.Input("spain"));
        cut.WaitForState(() => cut.FindAll(".country-dropdown-item").Count == 1);

        // Assert
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Single(items);
        Assert.Contains("Spain", items[0].TextContent);
    }

    [Fact]
    public void Shows_No_Countries_Found_Message_When_Search_Has_No_Results()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Type non-matching search
        var searchInput = cut.Find("input[type='text']");
        cut.InvokeAsync(() => searchInput.Input("xyz"));
        cut.WaitForState(() => cut.FindAll(".country-dropdown-item").Count == 0);

        // Assert
        var noResultsMessages = cut.FindAll(".text-muted");
        var noResultsMessage = noResultsMessages.First(m => m.TextContent.Contains("No countries found"));
        Assert.Contains("No countries found", noResultsMessage.TextContent);
    }

    [Fact]
    public void Clicking_Country_Selects_It_And_Closes_Dropdown()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Click Spain
        var spainItem = cut.FindAll(".country-dropdown-item")[1];
        cut.InvokeAsync(() => spainItem.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 0);

        // Assert - Dropdown closed
        Assert.Empty(cut.FindAll(".country-dropdown-menu"));

        // Assert - Spain is selected
        var selectedButton = cut.Find("button.form-select");
        Assert.Contains("Spain", selectedButton.TextContent);
        Assert.Contains("fi-ES", selectedButton.InnerHtml);
    }

    [Fact]
    public void Selecting_Country_Raises_ValueChanged()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        string? capturedValue = null;
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value)
            .Add(p => p.ValueChanged, newValue => capturedValue = newValue));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Click Portugal
        var portugalItem = cut.FindAll(".country-dropdown-item")[0];
        cut.InvokeAsync(() => portugalItem.Click());
        cut.WaitForState(() => capturedValue != null);

        // Assert
        Assert.Equal("Portugal", capturedValue);
    }

    [Fact]
    public void Clicking_Button_Again_Closes_Dropdown()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Act - Click button again
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 0);

        // Assert
        Assert.Empty(cut.FindAll(".country-dropdown-menu"));
    }

    [Fact]
    public void Closing_Dropdown_Clears_Search_Text()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown and search
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        var searchInput = cut.Find("input[type='text']");
        cut.InvokeAsync(() => searchInput.Input("Por"));
        cut.WaitForState(() => cut.FindAll(".country-dropdown-item").Count == 1);

        // Act - Close dropdown
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 0);

        // Act - Reopen dropdown
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert - All countries shown (search cleared)
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Selecting_Country_Clears_Search_Text()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" },
            new() { Code = "ES", Name = "Spain" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act - Open dropdown and search
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        var searchInput = cut.Find("input[type='text']");
        cut.InvokeAsync(() => searchInput.Input("Por"));
        cut.WaitForState(() => cut.FindAll(".country-dropdown-item").Count == 1);

        // Act - Select Portugal
        var portugalItem = cut.Find(".country-dropdown-item");
        cut.InvokeAsync(() => portugalItem.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 0);

        // Act - Reopen dropdown
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert - All countries shown (search cleared)
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Handles_Null_Countries_List()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, null)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert - Should not throw, shows placeholder
        var button = cut.Find("button.form-select");
        Assert.Contains("-- Select Country --", button.TextContent);
    }

    [Fact]
    public void Handles_Empty_Countries_List()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, new List<CountryInfo>())
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert
        var items = cut.FindAll(".country-dropdown-item");
        Assert.Empty(items);
        Assert.Contains("No countries found", cut.Markup);
    }

    [Fact]
    public void Country_Items_Display_Flag_And_Name()
    {
        // Arrange
        var countries = new List<CountryInfo>
        {
            new() { Code = "PT", Name = "Portugal" }
        };
        var value = string.Empty;

        var cut = Render<CountrySelector>(parameters => parameters
            .Add(p => p.Countries, countries)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var button = cut.Find("button.form-select");
        cut.InvokeAsync(() => button.Click());
        cut.WaitForState(() => cut.FindAll(".country-dropdown-menu").Count == 1);

        // Assert
        var item = cut.Find(".country-dropdown-item");
        Assert.Contains("fi-PT", item.InnerHtml);
        Assert.Contains("Portugal", item.TextContent);
    }
}
