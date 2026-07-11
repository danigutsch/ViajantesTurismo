using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class CustomerBookingCreateFormTests : BunitContext
{
    [Fact]
    public void Renders_companion_dropdown()
    {
        // Arrange
        var currentCustomerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: currentCustomerId, firstName: "Current", lastName: "Customer"),
            BuildCustomerDto(firstName: "Companion", lastName: "Person")
        };
        var model = new BookingFormModel();
        GetTourDto[] tours = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelect = cut.FindAll("select.form-select")[1]; // Second select
        var options = companionSelect.QuerySelectorAll("option");
        TestAssert.Equal(2, options.Length); // "No Companion" + 1 other customer
        TestAssert.Equal("-- No Companion --", options[0].TextContent);
        TestAssert.DoesNotContain(options, o => o.TextContent.Contains("Current Customer", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Companion Person", StringComparison.Ordinal));
    }

    [Fact]
    public void Companion_dropdown_excludes_current_customer()
    {
        // Arrange
        var currentCustomerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: currentCustomerId, firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith"),
            BuildCustomerDto(firstName: "Charlie", lastName: "Jones")
        };
        var model = new BookingFormModel();
        GetTourDto[] tours = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelect = cut.FindAll("select.form-select")[1];
        var options = companionSelect.QuerySelectorAll("option");
        TestAssert.Equal(3, options.Length); // Placeholder + 2 other customers (Alice excluded)
        TestAssert.DoesNotContain(options, o => o.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Charlie Jones", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_notes_textarea()
    {
        // Arrange
        var model = new BookingFormModel { Notes = "Special requirements" };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        TestAssert.Equal("Special requirements", notesTextArea.GetAttribute("value"));
        TestAssert.Equal("3", notesTextArea.GetAttribute("rows"));
    }

    [Fact]
    public void Renders_discount_card()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountCard = cut.FindAll(".card").First(c => c.TextContent.Contains("Discount", StringComparison.Ordinal));
        TestAssert.Contains("Discount (Optional)", discountCard.QuerySelector(".card-title")!.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-percent", discountCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_discounttype_dropdown()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountTypeSelect = cut.Find("select#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        TestAssert.Equal(3, options.Length);
        TestAssert.Contains("No Discount", options[0].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Percentage (0-100%)", options[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Absolute Amount (in tour currency)", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_discount_fields_when_discounttype_is_none()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.None };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var labels = cut.FindAll("label");
        TestAssert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Percentage", StringComparison.Ordinal));
        TestAssert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Amount", StringComparison.Ordinal));
        TestAssert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Reason", StringComparison.Ordinal));
    }

    [Fact]
    public void Shows_discount_percentage_fields_when_discounttype_is_percentage()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 10m,
            DiscountReason = "Loyalty discount"
        };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var percentageLabel = cut.Find("label:contains('Discount Percentage')");
        _ = TestAssert.NotNull(percentageLabel);

        var discountAmountInput = cut.Find("input#discountAmount");
        TestAssert.Equal("10", discountAmountInput.GetAttribute("value"));

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        _ = TestAssert.NotNull(helpText);

        var reasonTextArea = cut.Find("textarea#discountReason");
        TestAssert.Equal("Loyalty discount", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Shows_discount_amount_fields_when_discounttype_is_absolute()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 50m,
            DiscountReason = "Special promotion"
        };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var amountLabel = cut.Find("label:contains('Discount Amount')");
        _ = TestAssert.NotNull(amountLabel);
        TestAssert.DoesNotContain("Percentage", amountLabel.TextContent, StringComparison.Ordinal);

        var discountAmountInput = cut.Find("input#discountAmount");
        TestAssert.Equal("50", discountAmountInput.GetAttribute("value"));

        var helpText = cut.Find(".form-text:contains('Enter the discount amount in the tour currency')");
        _ = TestAssert.NotNull(helpText);

        var reasonTextArea = cut.Find("textarea#discountReason");
        TestAssert.Equal("Special promotion", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Renders_discount_reason_placeholder_and_help_text()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Percentage };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var reasonTextArea = cut.Find("textarea#discountReason");
        TestAssert.Equal("e.g., Early bird discount, Loyalty customer, Group discount",
            reasonTextArea.GetAttribute("placeholder"));
        TestAssert.Equal("2", reasonTextArea.GetAttribute("rows"));

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        TestAssert.Contains("10-500 characters", helpText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_create_button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        TestAssert.Contains("Create Booking", createButton.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-primary", createButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_cancel_button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        TestAssert.Contains("btn-secondary", cancelButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_button_shows_spinner_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        var spinner = createButton.QuerySelector(".spinner-border");
        _ = TestAssert.NotNull(spinner);
        TestAssert.Contains("spinner-border-sm", spinner.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Buttons_are_disabled_when_submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var createButton = cut.Find("button[type='submit']");
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        TestAssert.True(createButton.HasAttribute("disabled"));
        TestAssert.True(cancelButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnCancel_is_called_when_cancel_button_is_clicked()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];
        var cancelCalled = false;

        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.OnCancel, EventCallback.Factory.Create(this, () => cancelCalled = true)));

        // Act
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        await cancelButton.ClickAsync(new MouseEventArgs());

        // Assert
        TestAssert.True(cancelCalled);
    }

    [Fact]
    public void Renders_DataAnnotationsValidator()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        _ = TestAssert.NotNull(validator);
    }

    [Fact]
    public void Tour_and_companion_are_in_two_column_layout()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var row = cut.Find(".row");
        var columns = row.QuerySelectorAll(".col-md-6");
        TestAssert.Equal(2, columns.Length);

        var tourColumn = columns[0];
        TestAssert.Contains("Tour", tourColumn.TextContent, StringComparison.Ordinal);

        var companionColumn = columns[1];
        TestAssert.Contains("Companion", companionColumn.TextContent, StringComparison.Ordinal);
    }
}
