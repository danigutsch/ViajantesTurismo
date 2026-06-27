namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public sealed class CustomerSelectorTests : BunitContext
{
    [Fact]
    public void Renders_search_input_with_placeholder()
    {
        // Arrange
        GetCustomerDto[] customers = [];
        Guid? value = null;

        // Act
        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var input = cut.Find("input.form-control");
        Assert.Equal("Search customers by name or email...", input.GetAttribute("placeholder"));
    }

    [Fact]
    public void Dropdown_is_initially_closed()
    {
        // Arrange
        var customers = new List<GetCustomerDto> { BuildCustomerDto() };
        Guid? value = null;

        // Act
        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var dropdowns = cut.FindAll(".dropdown-menu");
        Assert.Empty(dropdowns);
    }

    [Fact]
    public void Opens_dropdown_on_focus()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "John", lastName: "Doe")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var dropdown = cut.Find(".dropdown-menu.show");
        Assert.NotNull(dropdown);
    }

    [Fact]
    public void Displays_first_10_customers_when_no_search_term()
    {
        // Arrange
        var customers = Enumerable.Range(1, 15)
            .Select(i => BuildCustomerDto(firstName: $"Customer{i}", lastName: "Test"))
            .ToList();
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Equal(10, items.Count);
    }

    [Fact]
    public void Displays_clear_selection_option()
    {
        // Arrange
        var customers = new List<GetCustomerDto> { BuildCustomerDto() };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var clearButton = cut.Find(".dropdown-item.text-muted");
        Assert.Contains("Clear selection", clearButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_customer_full_name_and_ID()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Jane", lastName: "Smith")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var customerButton = cut.Find(".dropdown-item:not(.text-muted)");
        Assert.Contains("Jane Smith", customerButton.TextContent, StringComparison.Ordinal);
        Assert.Contains($"ID: {customerId}", customerButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_customer_email_and_nationality()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(email: "test@example.com", nationality: "Canada")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var customerButton = cut.Find(".dropdown-item:not(.text-muted)");
        Assert.Contains("test@example.com", customerButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("Canada", customerButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Filters_by_first_name()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith"),
            BuildCustomerDto(firstName: "Charlie", lastName: "Johnson")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("alice");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Single(items);
        Assert.Contains("Alice Brown", items[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Filters_by_last_name()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith"),
            BuildCustomerDto(firstName: "Charlie", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("brown");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Filters_by_email()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", email: "alice@example.com"),
            BuildCustomerDto(firstName: "Bob", email: "bob@test.com"),
            BuildCustomerDto(firstName: "Charlie", email: "charlie@example.com")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("example");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Filters_by_customer_ID()
    {
        // Arrange
        var customerId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Alice"),
            BuildCustomerDto(firstName: "Bob")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("12345678");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Single(items);
        Assert.Contains("Alice", items[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Filter_is_case_insensitive()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("ALICE");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Single(items);
        Assert.Contains("Alice Brown", items[0].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Limits_filtered_results_to_20_items()
    {
        // Arrange
        var customers = Enumerable.Range(1, 30)
            .Select(i => BuildCustomerDto(firstName: "Test", lastName: $"Customer{i}"))
            .ToList();
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("test");

        // Assert
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Equal(20, items.Count);
    }

    [Fact]
    public void Displays_no_customers_found_message()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("nonexistent");

        // Assert
        var noResults = cut.Find(".dropdown-item-text.text-muted");
        Assert.Contains("No customers found", noResults.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Selecting_customer_updates_value()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value)
            .Add(p => p.ValueChanged, newValue => value = newValue));

        // Act
        var input = cut.Find("input");
        input.Focus();

        var customerButton = cut.Find(".dropdown-item:not(.text-muted)");
        customerButton.Click();

        // Assert
        Assert.Equal(customerId, value);
    }

    [Fact]
    public void Selecting_customer_closes_dropdown()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        var customerButton = cut.Find(".dropdown-item:not(.text-muted)");
        customerButton.Click();

        // Assert
        var dropdowns = cut.FindAll(".dropdown-menu.show");
        Assert.Empty(dropdowns);
    }

    [Fact]
    public void Selecting_customer_clears_search_term()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();
        input.Input("alice");

        var customerButton = cut.Find(".dropdown-item:not(.text-muted)");
        customerButton.Click();

        // Assert - Reopen dropdown to check search term was cleared
        input.Focus();
        var items = cut.FindAll(".dropdown-item:not(.text-muted):not(.dropdown-item-text)");
        Assert.Single(items); // Should show all (1) customer, not filtered
    }

    [Fact]
    public void Clear_selection_sets_value_to_null()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Alice")
        };
        Guid? value = customerId;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value)
            .Add(p => p.ValueChanged, newValue => value = newValue));

        // Act
        var input = cut.Find("input");
        input.Focus();

        var clearButton = cut.Find(".dropdown-item.text-muted");
        clearButton.Click();

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void Displays_selected_customer_badge_when_value_set()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Alice", lastName: "Brown")
        };
        Guid? value = customerId;

        // Act
        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var badge = cut.Find(".badge.bg-primary");
        Assert.Contains("Selected: Alice Brown", badge.TextContent, StringComparison.Ordinal);
        Assert.Contains($"ID: {customerId}", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_display_selected_badge_when_no_value()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown")
        };
        Guid? value = null;

        // Act
        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert
        var badges = cut.FindAll(".badge.bg-primary");
        Assert.Empty(badges);
    }

    [Fact]
    public void Highlights_selected_customer_in_dropdown()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Alice"),
            BuildCustomerDto(firstName: "Bob")
        };
        Guid? value = customerId;

        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, customers)
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Act
        var input = cut.Find("input");
        input.Focus();

        // Assert
        var activeItem = cut.Find(".dropdown-item.active");
        Assert.Contains("Alice", activeItem.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Handles_empty_customer_list()
    {
        // Arrange
        GetCustomerDto[] customers = [];
        Guid? value = null;

        // Act
        var cut = Render<CustomerSelector>(parameters => parameters
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.Value, value)
            .Add(p => p.ValueExpression, () => value));

        // Assert - Should render without errors
        var input = cut.Find("input.form-control");
        Assert.NotNull(input);
    }
}
