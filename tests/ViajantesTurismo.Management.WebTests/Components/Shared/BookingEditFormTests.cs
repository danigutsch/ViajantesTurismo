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
        (alert.TextContent).ShouldContain("Customer and companion cannot be changed after booking creation", StringComparison.Ordinal);
        (alert.InnerHtml).ShouldContain("bi-info-circle", StringComparison.Ordinal);
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
        (selects.Count).ShouldBe(2);
        var select = selects[0];
        var options = select.QuerySelectorAll("option");
        (options.Length).ShouldBe(3);
        (options[0].TextContent).ShouldBe("-- Select Customer --");
        (options[1].TextContent).ShouldContain("Alice Brown (alice@example.com)", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Bob Smith (bob@example.com)", StringComparison.Ordinal);
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
        (selects.Count).ShouldBe(2);

        var companionSelect = selects[1];
        var companionOptions = companionSelect.QuerySelectorAll("option");
        (companionOptions).ShouldNotBeEmpty();
        (companionOptions[0].TextContent).ShouldBe("-- No Companion --");

        var label = cut.Find("label:contains('Companion (Optional)')");
        _ = (label).ShouldNotBeNull();
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
        (options.Length).ShouldBe(3); // Placeholder + 2 non-selected customers
        (options).ShouldNotContain(opt => opt.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        (options).ShouldContain(opt => opt.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        (options).ShouldContain(opt => opt.TextContent.Contains("Charlie Davis", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_notes_textarea()
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
        (notesTextArea.GetAttribute("value")).ShouldBe("Test notes");
        (notesTextArea.GetAttribute("rows")).ShouldBe("3");
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
        (discountCard.QuerySelector(".card-title")!.TextContent).ShouldContain("Discount", StringComparison.Ordinal);
        (discountCard.QuerySelector(".card-header")!.InnerHtml).ShouldContain("bi-percent", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_discounttype_dropdown()
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
        (options.Length).ShouldBe(3);
        (options[0].TextContent).ShouldContain("No Discount", StringComparison.Ordinal);
        (options[1].TextContent).ShouldContain("Percentage (0-100%)", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Absolute Amount", StringComparison.Ordinal);
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
        ((Func<object?>)(() => cut.Find("input#discountAmount"))).ShouldThrow<ElementNotFoundException>();
        ((Func<object?>)(() => cut.Find("textarea#discountReason"))).ShouldThrow<ElementNotFoundException>();
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
        _ = (discountAmountInput).ShouldNotBeNull();
        _ = (discountReasonTextArea).ShouldNotBeNull();

        var label = cut.Find("label[for='discountAmount']");
        (label.TextContent).ShouldContain("Discount Percentage", StringComparison.Ordinal);

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        _ = (helpText).ShouldNotBeNull();
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
        _ = (discountAmountInput).ShouldNotBeNull();
        _ = (discountReasonTextArea).ShouldNotBeNull();

        var label = cut.Find("label[for='discountAmount']");
        (label.TextContent).ShouldContain("Discount Amount", StringComparison.Ordinal);
    }

    [Fact]
    public void DiscountReason_has_placeholder_and_helptext()
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
        (discountReasonTextArea.GetAttribute("placeholder")).ShouldContain("Early bird discount", StringComparison.Ordinal);

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        (helpText.TextContent).ShouldContain("10-500 characters", StringComparison.Ordinal);
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
        (updateButton.TextContent).ShouldContain("Update Booking", StringComparison.Ordinal);
        (updateButton.ClassName).ShouldContain("btn-primary", StringComparison.Ordinal);
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
        (cancelButton.ClassName).ShouldContain("btn-secondary", StringComparison.Ordinal);
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
        _ = (spinner).ShouldNotBeNull();
        (spinner.ClassName).ShouldContain("spinner-border-sm", StringComparison.Ordinal);
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
        (updateButton.IsDisabled()).ShouldBeTrue();
        (cancelButton.IsDisabled()).ShouldBeTrue();
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
        (cancelCalled).ShouldBeTrue();
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
        _ = (validator).ShouldNotBeNull();
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
        (validationMessages).ShouldNotBeEmpty(); // CustomerId and CompanionId
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
        (notesTextArea.GetAttribute("value")).ShouldBe("Test notes");

        var discountTypeSelect = cut.Find("select#discountType");
        (discountTypeSelect.GetAttribute("value")).ShouldBe("Percentage");

        var discountAmountInput = cut.Find("input#discountAmount");
        (discountAmountInput.GetAttribute("value")).ShouldContain("15.5", StringComparison.Ordinal);

        var discountReasonTextArea = cut.Find("textarea#discountReason");
        (discountReasonTextArea.GetAttribute("value")).ShouldBe("Loyalty customer discount");
    }
}
