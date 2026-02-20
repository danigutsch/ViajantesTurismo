using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class CustomerBookingCreateFormTests : BunitContext
{
    [Fact]
    public void Renders_Tour_Dropdown()
    {
        // Arrange
        var tours = new List<GetTourDto>
        {
            BuildTourDto(name: "Tour A", startDate: new DateTime(2025, 6, 1)),
            BuildTourDto(name: "Tour B", startDate: new DateTime(2025, 7, 1))
        };
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var tourSelect = cut.Find("select.form-select");
        var options = tourSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 tours
        Assert.Contains("Tour A (01/06/2025)", options[1].TextContent);

        Assert.Contains("Tour B (01/07/2025)", options[2].TextContent);
    }

    [Fact]
    public void Shows_Available_Spots_When_Tour_Selected()
    {
        // Arrange
        var tour = BuildTourDto(maxCustomers: 10, currentCustomerCount: 3);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel { TourId = tour.Id };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var availabilityText = cut.Find(".form-text");
        Assert.Contains("7 spots available", availabilityText.TextContent);
        Assert.Contains("3 / 10 booked", availabilityText.TextContent);
        var successSpan = availabilityText.QuerySelector(".text-success");
        Assert.NotNull(successSpan);
        Assert.Contains("bi-check-circle", successSpan.InnerHtml);
    }

    [Fact]
    public void Shows_Fully_Booked_Message_When_No_Spots()
    {
        // Arrange
        var tour = BuildTourDto(maxCustomers: 10, currentCustomerCount: 10);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel { TourId = tour.Id };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var availabilityText = cut.Find(".form-text");
        Assert.Contains("Tour is fully booked", availabilityText.TextContent);
        Assert.Contains("10 / 10", availabilityText.TextContent);
        var dangerSpan = availabilityText.QuerySelector(".text-danger");
        Assert.NotNull(dangerSpan);
        Assert.Contains("bi-x-circle", dangerSpan.InnerHtml);
    }

    [Fact]
    public void Shows_One_Spot_Available_Singular()
    {
        // Arrange
        var tour = BuildTourDto(maxCustomers: 10, currentCustomerCount: 9);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel { TourId = tour.Id };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var availabilityText = cut.Find(".form-text");
        Assert.Contains("1 spot available", availabilityText.TextContent);
        Assert.DoesNotContain("spots", availabilityText.TextContent);
    }

    [Fact]
    public void Renders_Companion_Dropdown()
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
        Assert.Equal(2, options.Length); // "No Companion" + 1 other customer
        Assert.Equal("-- No Companion --", options[0].TextContent);
        Assert.DoesNotContain(options, o => o.TextContent.Contains("Current Customer"));
        Assert.Contains(options, o => o.TextContent.Contains("Companion Person"));
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
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, currentCustomerId));

        // Assert
        var companionSelect = cut.FindAll("select.form-select")[1];
        var options = companionSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 other customers (Alice excluded)
        Assert.DoesNotContain(options, o => o.TextContent.Contains("Alice Brown"));
        Assert.Contains(options, o => o.TextContent.Contains("Bob Smith"));
        Assert.Contains(options, o => o.TextContent.Contains("Charlie Jones"));
    }

    [Fact]
    public void Renders_Notes_TextArea()
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
        Assert.Equal("Special requirements", notesTextArea.GetAttribute("value"));
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
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var discountCard = cut.FindAll(".card").First(c => c.TextContent.Contains("Discount"));
        Assert.Contains("Discount (Optional)", discountCard.QuerySelector(".card-title")!.TextContent);
        Assert.Contains("bi-percent", discountCard.InnerHtml);
    }

    [Fact]
    public void Renders_DiscountType_Dropdown()
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
        Assert.Equal(3, options.Length);
        Assert.Contains("No Discount", options[0].TextContent);
        Assert.Contains("Percentage (0-100%)", options[1].TextContent);
        Assert.Contains("Absolute Amount (in tour currency)", options[2].TextContent);
    }

    [Fact]
    public void Hides_Discount_Fields_When_DiscountType_Is_None()
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
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Percentage"));
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Amount"));
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Discount Reason"));
    }

    [Fact]
    public void Shows_Discount_Percentage_Fields_When_DiscountType_Is_Percentage()
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
        Assert.NotNull(percentageLabel);

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Equal("10", discountAmountInput.GetAttribute("value"));

        var helpText = cut.Find(".form-text:contains('Enter a value between 0 and 100')");
        Assert.NotNull(helpText);

        var reasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Loyalty discount", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Shows_Discount_Amount_Fields_When_DiscountType_Is_Absolute()
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
        Assert.NotNull(amountLabel);
        Assert.DoesNotContain("Percentage", amountLabel.TextContent);

        var discountAmountInput = cut.Find("input#discountAmount");
        Assert.Equal("50", discountAmountInput.GetAttribute("value"));

        var helpText = cut.Find(".form-text:contains('Enter the discount amount in the tour currency')");
        Assert.NotNull(helpText);

        var reasonTextArea = cut.Find("textarea#discountReason");
        Assert.Equal("Special promotion", reasonTextArea.GetAttribute("value"));
    }

    [Fact]
    public void Renders_Discount_Reason_Placeholder_And_Help_Text()
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
        Assert.Equal("e.g., Early bird discount, Loyalty customer, Group discount",
            reasonTextArea.GetAttribute("placeholder"));
        Assert.Equal("2", reasonTextArea.GetAttribute("rows"));

        var helpText = cut.Find(".form-text:contains('Required for audit purposes')");
        Assert.Contains("10-500 characters", helpText.TextContent);
    }

    [Fact]
    public void Renders_Create_Button()
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
        Assert.Contains("Create Booking", createButton.TextContent);
        Assert.Contains("btn-primary", createButton.ClassName);
    }

    [Fact]
    public void Renders_Cancel_Button()
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
        Assert.Contains("btn-secondary", cancelButton.ClassName);
    }

    [Fact]
    public void Create_Button_Shows_Spinner_When_Submitting()
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
        Assert.NotNull(spinner);
        Assert.Contains("spinner-border-sm", spinner.ClassName);
    }

    [Fact]
    public void Buttons_Are_Disabled_When_Submitting()
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
        Assert.True(createButton.HasAttribute("disabled"));
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
        var cut = Render<CustomerBookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
            .Add(p => p.CurrentCustomerId, Guid.NewGuid()));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Tour_And_Companion_Are_In_Two_Column_Layout()
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
        Assert.Equal(2, columns.Length);

        var tourColumn = columns[0];
        Assert.Contains("Tour", tourColumn.TextContent);

        var companionColumn = columns[1];
        Assert.Contains("Companion", companionColumn.TextContent);
    }
}
