using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public class BookingCreateFormTests : BunitContext
{
    [Fact]
    public void Renders_Tour_Dropdown_When_Tours_Available()
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
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var tourSelect = cut.Find("select.form-select");
        var options = tourSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 tours
        Assert.Contains("Tour A (06/01/2025)", options[1].TextContent);
        Assert.Contains("Tour B (07/01/2025)", options[2].TextContent);
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
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

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
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

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
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var availabilityText = cut.Find(".form-text");
        Assert.Contains("1 spot available", availabilityText.TextContent);
        Assert.DoesNotContain("spots", availabilityText.TextContent);
    }

    [Fact]
    public void Renders_Customer_Dropdown()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown", email: "alice@example.com"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith", email: "bob@example.com")
        };
        var model = new BookingFormModel();
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var selects = cut.FindAll("select.form-select");
        var customerSelect = selects.First(s => s.TextContent.Contains("-- Select Customer --"));
        var options = customerSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Contains("Alice Brown (alice@example.com)", options[1].TextContent);
        Assert.Contains("Bob Smith (bob@example.com)", options[2].TextContent);
    }

    [Fact]
    public void Renders_RoomType_Dropdown_With_Options()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var roomTypeSelect = cut.FindAll("select.form-select")
            .First(s => s.TextContent.Contains("Single Room"));
        var options = roomTypeSelect.QuerySelectorAll("option");
        Assert.Equal(2, options.Length);
        Assert.Contains("Single Room (Base Price)", options[0].TextContent);
        Assert.Contains("Double Room (Base Price + Supplement)", options[1].TextContent);
    }

    [Fact]
    public void Shows_Single_Occupancy_Badge_For_DoubleRoom_Without_Companion()
    {
        // Arrange
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleRoom,
            CompanionId = null
        };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var badge = cut.Find(".badge.bg-info");
        Assert.Contains("Single Occupancy", badge.TextContent);
        Assert.Contains("No companion selected", badge.TextContent);
    }

    [Fact]
    public void Renders_Principal_Bike_Dropdown()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var bikeSelect = cut.FindAll("select.form-select")
            .First(s => s.TextContent.Contains("Regular Bike") && s.TextContent.Contains("E-Bike"));
        var options = bikeSelect.QuerySelectorAll("option");
        Assert.Equal(3, options.Length); // Placeholder + 2 bike types
        Assert.Contains("Regular Bike", options[1].TextContent);
        Assert.Contains("E-Bike", options[2].TextContent);
    }

    [Fact]
    public void Hides_Companion_Section_For_SingleRoom()
    {
        // Arrange
        var model = new BookingFormModel { RoomType = RoomTypeDto.SingleRoom };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var labels = cut.FindAll("label");
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Companion (Optional)"));
    }

    [Fact]
    public void Shows_Companion_Section_For_DoubleRoom()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: "Alice", lastName: "Brown"),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith")
        };
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleRoom,
            CustomerId = customers[0].Id
        };
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var companionLabel = cut.Find("label:contains('Companion (Optional)')");
        Assert.NotNull(companionLabel);

        var companionSelect = cut.FindAll("select.form-select")
            .First(s => s.TextContent.Contains("-- No Companion --"));
        var options = companionSelect.QuerySelectorAll("option");
        Assert.Equal(2, options.Length); // "No Companion" + Bob (Alice excluded)
        Assert.DoesNotContain(options, o => o.TextContent.Contains("Alice Brown"));
        Assert.Contains(options, o => o.TextContent.Contains("Bob Smith"));
    }

    [Fact]
    public void Shows_Companion_Bike_When_Companion_Selected()
    {
        // Arrange
        var customer1 = BuildCustomerDto(firstName: "Alice");
        var customer2 = BuildCustomerDto(firstName: "Bob");
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleRoom,
            CustomerId = customer1.Id,
            CompanionId = customer2.Id
        };
        var customers = new List<GetCustomerDto> { customer1, customer2 };
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var labels = cut.FindAll("label");
        Assert.Contains(labels, l => l.TextContent.Contains("Companion Bike"));
    }

    [Fact]
    public void Hides_Companion_Bike_When_No_Companion()
    {
        // Arrange
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleRoom,
            CompanionId = null
        };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var labels = cut.FindAll("label");
        Assert.DoesNotContain(labels, l => l.TextContent.Contains("Companion Bike"));
    }

    [Fact]
    public void Renders_Notes_TextArea()
    {
        // Arrange
        var model = new BookingFormModel { Notes = "Special request" };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var notesTextArea = cut.Find("textarea#notes");
        Assert.Equal("Special request", notesTextArea.GetAttribute("value"));
        Assert.Equal("3", notesTextArea.GetAttribute("rows"));
    }

    [Fact]
    public void Renders_Discount_Card()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

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
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
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
    public void Shows_Price_Breakdown_When_Tour_Selected()
    {
        // Arrange
        var tour = BuildTourDto(price: 1000m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular
        };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var priceCards = cut.FindAll(".card");
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown"));
        Assert.Contains("Price Breakdown", priceCard.TextContent);
        Assert.Contains("bi-calculator", priceCard.InnerHtml);
        Assert.Contains("Subtotal", priceCard.TextContent);
        Assert.Contains("Final Total", priceCard.TextContent);
    }

    [Fact]
    public void Price_Breakdown_Shows_Discount_When_Applied()
    {
        // Arrange
        var tour = BuildTourDto(price: 1000m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            DiscountType = DiscountTypeDto.Percentage,
            DiscountAmount = 10m
        };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var priceCards = cut.FindAll(".card");
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown"));
        Assert.Contains("Discount", priceCard.TextContent);
        Assert.Contains("(10.00%)", priceCard.TextContent);
        var discountRows = priceCard.QuerySelectorAll(".text-danger");
        Assert.NotEmpty(discountRows);
    }

    [Fact]
    public void Shows_Warning_When_Final_Price_Is_Zero_Or_Negative()
    {
        // Arrange
        var tour = BuildTourDto(
            price: 100m,
            regularBikePrice: 0m,
            eBikePrice: 0m,
            doubleRoomSupplementPrice: 0m);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular,
            DiscountType = DiscountTypeDto.Absolute,
            DiscountAmount = 150m
        };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var priceCards = cut.FindAll(".card");
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown"));
        var warning = priceCard.QuerySelector(".alert.alert-warning");
        Assert.NotNull(warning);
        Assert.Contains("Final price cannot be zero or negative", warning.TextContent);
        Assert.Contains("bi-exclamation-triangle", warning.InnerHtml);
    }

    [Fact]
    public void Hides_Price_Breakdown_When_No_Tour_Selected()
    {
        // Arrange
        var model = new BookingFormModel { TourId = null };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var cards = cut.FindAll(".card");
        Assert.DoesNotContain(cards, c => c.TextContent.Contains("Price Breakdown"));
    }

    [Fact]
    public void Renders_Create_Button()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

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
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var cancelButton = cut.Find("button[type='button']:contains('Cancel')");
        Assert.Contains("btn-secondary", cancelButton.ClassName);
    }

    [Fact]
    public void Create_Button_Shows_Spinner_When_Submitting()
    {
        // Arrange
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
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
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers])
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
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];
        var cancelCalled = false;

        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
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
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var validator = cut.FindComponent<DataAnnotationsValidator>();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Displays_Price_Breakdown_With_Currency_Symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1500m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.SingleRoom,
            PrincipalBikeType = BikeTypeDto.Regular
        };
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var priceCards = cut.FindAll(".card");
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown"));
        Assert.Contains("€", priceCard.TextContent);
    }
}
