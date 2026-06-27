using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class BookingEditFormTests : BunitContext
{
    [Fact]
    public void Renders_info_alert()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("Customer and companion cannot be changed after booking creation", alert.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-info-circle", alert.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customer_dropdown_disabled()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown", email: "alice@example.com"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith", email: "bob@example.com")
        };
        var model = new BookingFormModel { CustomerId = customers[0].Id };

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var selects = cut.FindAll("select.form-select[disabled]");
        Assert.Equal(2, selects.Count);
        var select = selects[0];
        var options = select.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Equal("-- Select Customer --", options[0].TextContent);
        Assert.Contains("Alice Brown (alice@example.com)", options[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Bob Smith (bob@example.com)", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_companion_dropdown_disabled()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith")
        };
        var model = new BookingFormModel { CustomerId = customers[0].Id };

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var selects = cut.FindAll("select.form-select[disabled]");
        Assert.Equal(2, selects.Count);

        var companionSelect = selects[1];
        var companionOptions = companionSelect.QuerySelectorAll("option");
        Assert.NotEmpty(companionOptions);
        Assert.Equal("-- No Companion --", companionOptions[0].TextContent);

        var label = cut.Find("label:contains('Companion (Optional)')");
        Assert.NotNull(label);
    }

    [Fact]
    public void Companion_dropdown_excludes_selected_customer()
    {
        // Arrange
        var customer1 = BuildCustomerDto(firstName: "Alice", lastName: "Brown");
        var customer2 = BuildCustomerDto(firstName: "Bob", lastName: "Smith");
        var customer3 = BuildCustomerDto(firstName: "Charlie", lastName: "Davis");
        var customers = new List<GetCustomerDto> { customer1, customer2, customer3 };
        var model = new BookingFormModel { CustomerId = customer1.Id };

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var companionSelect = cut.FindAll("select.form-select[disabled]")[1];
        var options = companionSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 non-selected customers
        Assert.DoesNotContain(options, opt => opt.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        Assert.Contains(options, opt => opt.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        Assert.Contains(options, opt => opt.TextContent.Contains("Charlie Davis", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_notes_textArea()
    {
        // Arrange
        var model = new BookingFormModel { Notes = "Test notes" };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        Assert.Equal("Test notes", notesTextArea.GetAttribute("value"));
        Assert.Equal("3", notesTextArea.GetAttribute("rows"));
    }

    [Fact]
    public void Renders_discount_card()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var discountCard = cut.Find(".card");
        Assert.Contains("Discount", discountCard.QuerySelector(".card-title")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-percent", discountCard.QuerySelector(".card-header")!.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_discountType_dropdown()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var discountTypeSelect = cut.Find("select#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Contains("No Discount", options[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Percentage (0-100%)", options[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Absolute Amount", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_discount_fields_when_type_is_none()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.None };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        Assert.Throws<ElementNotFoundException>(() => cut.Find("input#discountAmount"));
        Assert.Throws<ElementNotFoundException>(() => cut.Find("textarea#discountReason"));
    }

    [Fact]
    public void Shows_discount_fields_when_type_is_percentage()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Percentage };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var discountAmountInput = cut.Find("input#discountAmount");
        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.NotNull(discountAmountInput);
        Assert.NotNull(discountReasonTextArea);

        var label = cut.Find("label[for='discountAmount']");
        Assert.Contains("Discount Percentage", label.TextContent, StringComparison.Ordinal);

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        Assert.NotNull(helpText);
    }

    [Fact]
    public void Shows_discount_fields_when_type_is_absolute()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Absolute };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var discountAmountInput = cut.Find("input#discountAmount");
        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.NotNull(discountAmountInput);
        Assert.NotNull(discountReasonTextArea);

        var label = cut.Find("label[for='discountAmount']");
        Assert.Contains("Discount Amount", label.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscountReason_has_placeholder_and_helpText()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Percentage };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.Contains("Early bird discount", discountReasonTextArea.GetAttribute("placeholder"), StringComparison.Ordinal);

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        Assert.Contains("10-500 characters", helpText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_update_button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        Assert.Contains("Update Booking", updateButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("btn-primary", updateButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_cancel_button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        Assert.Contains("btn-secondary", cancelButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_button_shows_spinner_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.IsSubmitting, true));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        var spinner = updateButton.QuerySelector(".spinner-border");
        Assert.NotNull(spinner);
        Assert.Contains("spinner-border-sm", spinner.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Buttons_are_disabled_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.IsSubmitting, true));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        Assert.True(updateButton.IsDisabled());
        Assert.True(cancelButton.IsDisabled());
    }

    [Fact]
    public async Task OnCancel_is_called_when_cancel_button_is_clicked()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        var cancelCalled = false;

        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        await cancelButton.ClickAsync(new MouseEventArgs());

        // Assert
        Assert.True(cancelCalled);
    }

    [Fact]
    public void Renders_DataAnnotationsValidator()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Renders_ValidationMessage_components()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var validationMessages = cut.FindComponents<ValidationMessage<Guid?>>();
        Assert.NotEmpty(validationMessages); // CustomerId and CompanionId
    }

    [Fact]
    public void Preloads_model_values()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var model = new BookingFormModel
        {
            CustomerId = customerId,
            Notes = "Test notes",
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 15.50m,
            DiscountReason = "Loyalty customer discount"
        };
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: customerId, firstName: "Test", lastName: "User")
        };

        // Act
        var cut = Render<BookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        Assert.Equal("Test notes", notesTextArea.GetAttribute("value"));

        var discountTypeSelect = cut.Find("select#discountType");
        Assert.Equal("Percentage", discountTypeSelect.GetAttribute("value"));

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Contains("15.5", discountAmountInput.GetAttribute("value"), StringComparison.Ordinal);

        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Loyalty customer discount", discountReasonTextArea.GetAttribute("value"));
    }
}
