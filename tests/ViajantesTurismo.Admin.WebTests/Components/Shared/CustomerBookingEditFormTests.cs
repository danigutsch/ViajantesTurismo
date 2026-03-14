using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.Builders.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class CustomerBookingEditFormTests : BunitContext
{
    [Fact]
    public void Renders_Info_Alert()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var alert = cut.Find(".alert.alert-info");
        Assert.Contains("Tour and companion cannot be changed after booking creation", alert.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-info-circle", alert.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Tour_Dropdown_As_Disabled()
    {
        // Arrange
        var tours = new List<GetTourDto>
        {
            BuildTourDto(name: "Tour A", startDate: new DateTime(2025, 6, 1)),
            BuildTourDto(name: "Tour B", startDate: new DateTime(2025, 7, 1))
        };
        var model = new BookingFormModel { TourId = tours[0].Id };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var tourSelect = cut.Find("select.form-select[disabled]");
        Assert.True(tourSelect.HasAttribute("disabled"));
        var options = tourSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 tours
        Assert.Contains("Tour A (01/06/2025)", options[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Tour B (01/07/2025)", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Companion_Dropdown_As_Disabled()
    {
        // Arrange
        var currentCustomerId = Guid.NewGuid();
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(id: currentCustomerId, firstName: "Current", lastName: "Customer"),
            BuildCustomerDto(firstName: "Companion", lastName: "Person")
        };
        var model = new BookingFormModel { CompanionId = customers[1].Id };
        GetTourDto[] tours = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelects = cut.FindAll("select.form-select[disabled]");
        var companionSelect = companionSelects[1]; // Second disabled select
        Assert.True(companionSelect.HasAttribute("disabled"));
        var options = companionSelect.QuerySelectorAll("option");
        Assert.Equal(2, options.Length); // "No Companion" + 1 other customer
        Assert.Equal("-- No Companion --", options[0].TextContent);
        Assert.DoesNotContain(options, o => o.TextContent.Contains("Current Customer", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Companion Person", StringComparison.Ordinal));
    }

    [Fact]
    public void Companion_Dropdown_Excludes_Current_Customer()
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
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelects = cut.FindAll("select.form-select[disabled]");
        var companionSelect = companionSelects[1];
        var options = companionSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 other customers (Alice excluded)
        Assert.DoesNotContain(options, o => o.TextContent.Contains("Alice Brown", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
        Assert.Contains(options, o => o.TextContent.Contains("Charlie Jones", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_Notes_TextArea()
    {
        // Arrange
        var model = new BookingFormModel { Notes = "Special dietary requirements" };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        Assert.Equal("Special dietary requirements", notesTextArea.GetAttribute("value"));
        Assert.Equal("3", notesTextArea.GetAttribute("rows"));
    }

    [Fact]
    public void Renders_Discount_Card()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountCard = cut.FindAll(".card").First(c => c.TextContent.Contains("Discount", StringComparison.Ordinal));
        Assert.Contains("Discount", discountCard.QuerySelector(".card-title")!.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-percent", discountCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_DiscountType_Dropdown()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountTypeSelect = cut.Find("select#discountType");
        var options = discountTypeSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Contains("No Discount", options[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Percentage (0-100%)", options[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Absolute Amount", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_Discount_Fields_When_DiscountType_Is_None()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.None };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var labels = cut.FindAll("label");
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Percentage", StringComparison.Ordinal));
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Amount", StringComparison.Ordinal));
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Reason", StringComparison.Ordinal));
    }

    [Fact]
    public void Shows_Discount_Percentage_Fields_When_DiscountType_Is_Percentage()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 15m,
            DiscountReason = "Early bird special"
        };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var percentageLabel = cut.Find("label:contains('Discount Percentage')");
        Assert.NotNull(percentageLabel);

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Equal("15", discountAmountInput.GetAttribute("value"));

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        Assert.NotNull(helpText);

        var reasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Early bird special", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Shows_Discount_Amount_Fields_When_DiscountType_Is_Absolute()
    {
        // Arrange
        var model = new BookingFormModel
        {
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 100m,
            DiscountReason = "Group discount"
        };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var amountLabel = cut.Find("label:contains('Discount Amount')");
        Assert.NotNull(amountLabel);
        Assert.DoesNotContain("Percentage", amountLabel.TextContent, StringComparison.Ordinal);

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Equal("100", discountAmountInput.GetAttribute("value"));

        var reasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Group discount", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Renders_Discount_Reason_Placeholder_And_Help_Text()
    {
        // Arrange
        var model = new BookingFormModel { DiscountType = DiscountTypeDto.Percentage };
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var reasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("e.g., Early bird discount, Loyalty customer, Group discount",
            reasonTextArea.GetAttribute("placeholder"));
        Assert.Equal("2", reasonTextArea.GetAttribute("rows"));

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        Assert.Contains("10-500 characters", helpText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Update_Button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        Assert.Contains("Update Booking", updateButton.TextContent, StringComparison.Ordinal);
        Assert.Contains("btn-primary", updateButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Cancel_Button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        Assert.Contains("btn-secondary", cancelButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_Button_Shows_Spinner_When_Submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        var spinner = updateButton.QuerySelector(".spinner-border");
        Assert.NotNull(spinner);
        Assert.Contains("spinner-border-sm", spinner.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Buttons_Are_Disabled_When_Submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
            .Add(p => p.IsSubmitting, true));

        // Assert
        var updateButton = cut.Find("button[type='submit']");
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        Assert.True(updateButton.HasAttribute("disabled"));
        Assert.True(cancelButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnCancel_Is_Called_When_Cancel_Button_Is_Clicked()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];
        var cancelCalled = false;

        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid())
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
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Preloads_Model_Values()
    {
        // Arrange
        var tour = BuildTourDto(name: "Selected Tour");
        var companion = BuildCustomerDto(firstName: "Companion", lastName: "Customer");
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            CompanionId = companion.Id,
            Notes = "Preloaded notes",
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 20m,
            DiscountReason = "Preloaded reason"
        };
        var tours = new List<GetTourDto> { tour };
        var customers = new List<GetCustomerDto> { companion };

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var tourSelect = cut.Find("select.form-select[disabled]");
        Assert.Equal(tour.Id.ToString(), tourSelect.GetAttribute("value"));

        var notesTextArea = cut.Find("textarea#notes");
        Assert.Equal("Preloaded notes", notesTextArea.GetAttribute("value"));

        var discountTypeSelect = cut.Find("select#discountType");
        Assert.Equal(nameof(DiscountTypeDto.Percentage), discountTypeSelect.GetAttribute("value"));

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Equal("20", discountAmountInput.GetAttribute("value"));

        var discountReasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Preloaded reason", discountReasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Tour_And_Companion_Dropdowns_Are_In_Two_Column_Layout()
    {
        // Arrange
        var model = new BookingFormModel();
        GetTourDto[] tours = [];
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingEditForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var row = cut.Find(".row");
        var columns = row.QuerySelectorAll(".col-md-6");
        Assert.Equal(2, columns.Length);

        var tourColumn = columns[0];
        Assert.Contains("Tour", tourColumn.TextContent, StringComparison.Ordinal);

        var companionColumn = columns[1];
        Assert.Contains("Companion", companionColumn.TextContent, StringComparison.Ordinal);
    }
}
