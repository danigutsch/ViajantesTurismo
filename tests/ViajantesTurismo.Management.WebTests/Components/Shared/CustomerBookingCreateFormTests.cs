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
        (options.Length).ShouldBe(2); // "No Companion" + 1 other customer
        (options[0].TextContent).ShouldBe("-- No Companion --");
        (options).ShouldNotContain(o => o.TextContent.Contains("Current Customer", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Companion Person", StringComparison.Ordinal));
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
        (options.Length).ShouldBe(3); // Placeholder + 2 other customers (Alice excluded)
        (options).ShouldNotContain(o => o.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Charlie Jones", StringComparison.Ordinal));
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
        (notesTextArea.GetAttribute("value")).ShouldBe("Special requirements");
        (notesTextArea.GetAttribute("rows")).ShouldBe("3");
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
        (discountCard.QuerySelector(".card-title")!.TextContent).ShouldContain("Discount (Optional)", StringComparison.Ordinal);
        (discountCard.InnerHtml).ShouldContain("bi-percent", StringComparison.Ordinal);
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
        (options.Length).ShouldBe(3);
        (options[0].TextContent).ShouldContain("No Discount", StringComparison.Ordinal);
        (options[1].TextContent).ShouldContain("Percentage (0-100%)", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Absolute Amount (in tour currency)", StringComparison.Ordinal);
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
        (labels).ShouldNotContain(l => l.TextContent.Contains("Discount Percentage", StringComparison.Ordinal));
        (labels).ShouldNotContain(l => l.TextContent.Contains("Discount Amount", StringComparison.Ordinal));
        (labels).ShouldNotContain(l => l.TextContent.Contains("Discount Reason", StringComparison.Ordinal));
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
        _ = (percentageLabel).ShouldNotBeNull();

        var discountAmountInput = cut.Find("input#discountAmount");
        (discountAmountInput.GetAttribute("value")).ShouldBe("10");

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        _ = (helpText).ShouldNotBeNull();

        var reasonTextArea = cut.Find("textarea#discountReason");
        (reasonTextArea.GetAttribute("value")).ShouldBe("Loyalty discount");
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
        _ = (amountLabel).ShouldNotBeNull();
        (amountLabel.TextContent).ShouldNotContain("Percentage", StringComparison.Ordinal);

        var discountAmountInput = cut.Find("input#discountAmount");
        (discountAmountInput.GetAttribute("value")).ShouldBe("50");

        var helpText = cut.Find(".form-text:contains('Enter the discount amount in the tour currency')");
        _ = (helpText).ShouldNotBeNull();

        var reasonTextArea = cut.Find("textarea#discountReason");
        (reasonTextArea.GetAttribute("value")).ShouldBe("Special promotion");
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
        (reasonTextArea.GetAttribute("placeholder")).ShouldBe("e.g., Early bird discount, Loyalty customer, Group discount");
        (reasonTextArea.GetAttribute("rows")).ShouldBe("2");

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        (helpText.TextContent).ShouldContain("10-500 characters", StringComparison.Ordinal);
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
        (createButton.TextContent).ShouldContain("Create Booking", StringComparison.Ordinal);
        (createButton.ClassName).ShouldContain("btn-primary", StringComparison.Ordinal);
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
        (cancelButton.ClassName).ShouldContain("btn-secondary", StringComparison.Ordinal);
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
        _ = (spinner).ShouldNotBeNull();
        (spinner.ClassName).ShouldContain("spinner-border-sm", StringComparison.Ordinal);
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
        (createButton.HasAttribute("disabled")).ShouldBeTrue();
        (cancelButton.HasAttribute("disabled")).ShouldBeTrue();
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
        (cancelCalled).ShouldBeTrue();
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
        _ = (validator).ShouldNotBeNull();
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
        (columns.Length).ShouldBe(2);

        var tourColumn = columns[0];
        (tourColumn.TextContent).ShouldContain("Tour", StringComparison.Ordinal);

        var companionColumn = columns[1];
        (companionColumn.TextContent).ShouldContain("Companion", StringComparison.Ordinal);
    }
}
