using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class BookingEditFormTests : BunitContext
{
    [Fact]
    public void Renders_Info_Alert()
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
        Assert.Contains("Customer and companion cannot be changed after booking creation", alert.TextContent);
        Assert.Contains("bi-info-circle", alert.InnerHtml);
    }

    [Fact]
    public void Renders_Customer_Dropdown_Disabled()
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
        Assert.Contains("Alice Brown (alice@example.com)", options[1].TextContent);
        Assert.Contains("Bob Smith (bob@example.com)", options[2].TextContent);
    }

    [Fact]
    public void Renders_Companion_Dropdown_Disabled()
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
    public void Companion_Dropdown_Excludes_Selected_Customer()
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
        Assert.DoesNotContain(options, opt => opt.TextContent.Contains("Alice Brown"));
        Assert.Contains(options, opt => opt.TextContent.Contains("Bob Smith"));
        Assert.Contains(options, opt => opt.TextContent.Contains("Charlie Davis"));
    }

    [Fact]
    public void Renders_Notes_TextArea()
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
    public void Renders_Discount_Card()
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
        Assert.Contains("Discount", discountCard.QuerySelector(".card-title")!.TextContent);
        Assert.Contains("bi-percent", discountCard.QuerySelector(".card-header")!.InnerHtml);
    }

    [Fact]
    public void Renders_DiscountType_Dropdown()
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
        Assert.Contains("No Discount", options[0].TextContent);
        Assert.Contains("Percentage (0-100%)", options[1].TextContent);
        Assert.Contains("Absolute Amount", options[2].TextContent);
    }

    [Fact]
    public void Hides_Discount_Fields_When_Type_Is_None()
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
    public void Shows_Discount_Fields_When_Type_Is_Percentage()
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
        Assert.Contains("Discount Percentage", label.TextContent);

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        Assert.NotNull(helpText);
    }

    [Fact]
    public void Shows_Discount_Fields_When_Type_Is_Absolute()
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
        Assert.Contains("Discount Amount", label.TextContent);
    }

    [Fact]
    public void DiscountReason_Has_Placeholder_And_HelpText()
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
        Assert.Contains("Early bird discount", discountReasonTextArea.GetAttribute("placeholder"));

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        Assert.Contains("10-500 characters", helpText.TextContent);
    }

    [Fact]
    public void Renders_Update_Button()
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
        Assert.Contains("Update Booking", updateButton.TextContent);
        Assert.Contains("btn-primary", updateButton.ClassName);
    }

    [Fact]
    public void Renders_Cancel_Button()
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
        Assert.Contains("btn-secondary", cancelButton.ClassName);
    }

    [Fact]
    public void Update_Button_Shows_Spinner_When_Submitting()
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
        Assert.Contains("spinner-border-sm", spinner.ClassName);
    }

    [Fact]
    public void Buttons_Are_Disabled_When_Submitting()
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
    public async Task OnCancel_Is_Called_When_Cancel_Button_Is_Clicked()
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
    public void Renders_ValidationMessage_Components()
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
    public void Preloads_Model_Values()
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
        Assert.Contains("15.5", discountAmountInput.GetAttribute("value"));

        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Loyalty customer discount", discountReasonTextArea.GetAttribute("value"));
    }
}
