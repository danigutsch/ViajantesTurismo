namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public sealed class CustomerSelectorTests : BunitContext
{
    [Fact]
    public void Renders_Search_Input_With_Placeholder()
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
    public void Dropdown_Is_Initially_Closed()
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
    public void Opens_Dropdown_On_Focus()
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
    public void Displays_First_10_Customers_When_No_Search_Term()
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
    public void Displays_Clear_Selection_Option()
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
    public void Displays_Customer_Full_Name_And_ID()
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
    public void Displays_Customer_Email_And_Nationality()
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
    public void Filters_By_First_Name()
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
    public void Filters_By_Last_Name()
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
    public void Filters_By_Email()
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
    public void Filters_By_Customer_ID()
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
    public void Filter_Is_Case_Insensitive()
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
    public void Limits_Filtered_Results_To_20_Items()
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
    public void Displays_No_Customers_Found_Message()
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
    public void Selecting_Customer_Updates_Value()
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
    public void Selecting_Customer_Closes_Dropdown()
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
    public void Selecting_Customer_Clears_Search_Term()
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
    public void Clear_Selection_Sets_Value_To_Null()
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
    public void Displays_Selected_Customer_Badge_When_Value_Set()
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
    public void Does_Not_Display_Selected_Badge_When_No_Value()
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
    public void Highlights_Selected_Customer_In_Dropdown()
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
    public void Handles_Empty_Customer_List()
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
