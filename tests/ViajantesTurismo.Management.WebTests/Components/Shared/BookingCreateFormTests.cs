using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public class BookingCreateFormTests : BunitContext
{
    private const string SelectSelector = "select.form-select";
    private const string LabelSelector = "label";
    private const string ValueAttributeName = "value";
    private const string CompanionBikeLabel = "Companion Bike";
    private const string CompanionOptionalLabel = "Companion (Optional)";
    private const string AliceFirstName = "Alice";
    private const string AliceLastName = "Brown";
    private const string AliceEmail = "alice@example.com";
    private const string AliceDisplayName = AliceFirstName + " " + AliceLastName;
    private const string AliceCustomerDisplayName = AliceDisplayName + " (" + AliceEmail + ")";

    [Fact]
    public void Renders_tour_dropdown_when_tours_available()
    {
        // Arrange
        var tours = new List<GetTourDto>
        {
            BuildTourDto(name: "Tour A", startDate: new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc)),
            BuildTourDto(name: "Tour B", startDate: new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc))
        };
        var model = new BookingFormModel();
        GetCustomerDto[] customers = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var tourSelect = cut.Find(SelectSelector);
        var options = tourSelect.QuerySelectorAll("option");
        TestAssert.Equal(3, options.Length); // Placeholder + 2 tours
        TestAssert.Contains("Tour A (01/06/2025)", options[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Tour B (01/07/2025)", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_available_spots_when_tour_selected()
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
        TestAssert.Contains("7 spots available", availabilityText.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("3 / 10 booked", availabilityText.TextContent, StringComparison.Ordinal);
        var successSpan = availabilityText.QuerySelector(".text-success");
        _ = TestAssert.NotNull(successSpan);
        TestAssert.Contains("bi-check-circle", successSpan.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_fully_booked_message_when_no_spots()
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
        TestAssert.Contains("Tour is fully booked", availabilityText.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("10 / 10", availabilityText.TextContent, StringComparison.Ordinal);
        var dangerSpan = availabilityText.QuerySelector(".text-danger");
        _ = TestAssert.NotNull(dangerSpan);
        TestAssert.Contains("bi-x-circle", dangerSpan.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_one_spot_available_singular()
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
        TestAssert.Contains("1 spot available", availabilityText.TextContent, StringComparison.Ordinal);
        TestAssert.DoesNotContain("spots", availabilityText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customer_dropdown()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: AliceFirstName, lastName: AliceLastName, email: AliceEmail),
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
        var selects = cut.FindAll(SelectSelector);
        var customerSelect = selects.First(s => s.TextContent.Contains("-- Select Customer --", StringComparison.Ordinal));
        var options = customerSelect.QuerySelectorAll("option");
        TestAssert.Equal(3, options.Length);
        TestAssert.Contains(AliceCustomerDisplayName, options[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Bob Smith (bob@example.com)", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_roomtype_dropdown_with_options()
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
        var roomTypeSelect = cut.FindAll(SelectSelector)
            .First(s => s.TextContent.Contains("Single Room", StringComparison.Ordinal));
        var options = roomTypeSelect.QuerySelectorAll("option");
        TestAssert.Equal(2, options.Length);
        TestAssert.Contains("Double Room (Base Price)", options[0].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Single Room (Base Price + Supplement)", options[1].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_single_occupancy_badge_for_singleroom_without_companion()
    {
        // Arrange
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.SingleOccupancy,
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
        TestAssert.Contains("Single Occupancy", badge.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("No companion selected", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_principal_bike_dropdown()
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
        var bikeSelect = cut.FindAll(SelectSelector)
            .First(s => s.TextContent.Contains("Regular Bike", StringComparison.Ordinal) && s.TextContent.Contains("E-Bike", StringComparison.Ordinal));
        var options = bikeSelect.QuerySelectorAll("option");
        TestAssert.Equal(3, options.Length); // Placeholder + 2 bike types
        TestAssert.Contains("Regular Bike", options[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("E-Bike", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_companion_section_for_singleroom()
    {
        // Arrange
        var model = new BookingFormModel { RoomType = RoomTypeDto.SingleOccupancy };
        GetCustomerDto[] customers = [];
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var labels = cut.FindAll(LabelSelector);
        TestAssert.DoesNotContain(labels, l => l.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));
    }

    [Fact]
    public void Shows_companion_section_for_doubleroom()
    {
        // Arrange
        var customers = new List<GetCustomerDto>
        {
            BuildCustomerDto(firstName: AliceFirstName, lastName: AliceLastName),
            BuildCustomerDto(firstName: "Bob", lastName: "Smith")
        };
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
            CustomerId = customers[0].Id
        };
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        var companionLabel = cut.Find($"{LabelSelector}:contains('{CompanionOptionalLabel}')");
        _ = TestAssert.NotNull(companionLabel);

        var companionSelect = cut.FindAll(SelectSelector)
            .First(s => s.TextContent.Contains("-- No Companion --", StringComparison.Ordinal));
        var options = companionSelect.QuerySelectorAll("option");
        TestAssert.Equal(2, options.Length); // "No Companion" + Bob (Alice excluded)
        TestAssert.DoesNotContain(options, o => o.TextContent.Contains(AliceDisplayName, StringComparison.Ordinal));
        TestAssert.Contains(options, o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
    }

    [Fact]
    public void Shows_companion_bike_when_companion_selected()
    {
        // Arrange
        var customer1 = BuildCustomerDto(firstName: AliceFirstName);
        var customer2 = BuildCustomerDto(firstName: "Bob");
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var labels = cut.FindAll(LabelSelector);
        TestAssert.Contains(labels, l => l.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
    }

    [Fact]
    public void Selecting_customers_and_toggling_room_type_should_update_companion_fields_and_bike_selection()
    {
        // Arrange
        var principalCustomer = BuildCustomerDto(firstName: AliceFirstName, lastName: AliceLastName, email: AliceEmail, bikeType: BikeTypeDto.EBike);
        var companionCustomer = BuildCustomerDto(firstName: "Bob", lastName: "Smith", email: "bob@example.com", bikeType: BikeTypeDto.Regular);
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy
        };
        var customers = new List<GetCustomerDto> { principalCustomer, companionCustomer };
        GetTourDto[] tours = [];

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        cut.FindAll(SelectSelector)[0].Change(principalCustomer.Id);
        cut.FindAll(SelectSelector)[3].Change(companionCustomer.Id);

        // Assert
        var selectsAfterCompanionSelection = cut.FindAll(SelectSelector);
        TestAssert.Equal("EBike", selectsAfterCompanionSelection[2].GetAttribute(ValueAttributeName));
        TestAssert.Equal("Regular", selectsAfterCompanionSelection[4].GetAttribute(ValueAttributeName));

        // Act
        selectsAfterCompanionSelection[1].Change(RoomTypeDto.SingleOccupancy);

        // Assert
        var labelsAfterSingleRoom = cut.FindAll(LabelSelector);
        TestAssert.DoesNotContain(labelsAfterSingleRoom, label => label.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));
        TestAssert.DoesNotContain(labelsAfterSingleRoom, label => label.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
        TestAssert.Contains("Single Occupancy", cut.Find(".badge.bg-info").TextContent, StringComparison.Ordinal);
        TestAssert.Null(model.CompanionId);
        TestAssert.Null(model.CompanionBikeType);

        // Act
        cut.FindAll(SelectSelector)[1].Change(RoomTypeDto.DoubleOccupancy);

        // Assert
        var labelsAfterReturningToDouble = cut.FindAll(LabelSelector);
        TestAssert.Contains(labelsAfterReturningToDouble, label => label.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));

        var companionSelect = cut.FindAll(SelectSelector)[3];
        TestAssert.Equal(string.Empty, companionSelect.GetAttribute(ValueAttributeName) ?? string.Empty);
        TestAssert.DoesNotContain(cut.FindAll(LabelSelector), label => label.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
    }

    [Fact]
    public void Hides_companion_bike_when_no_companion()
    {
        // Arrange
        var model = new BookingFormModel
        {
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var labels = cut.FindAll(LabelSelector);
        TestAssert.DoesNotContain(labels, l => l.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_notes_textarea()
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
        TestAssert.Equal("Special request", notesTextArea.GetAttribute(ValueAttributeName));
        TestAssert.Equal("3", notesTextArea.GetAttribute("rows"));
    }

    [Fact]
    public void Renders_discount_card()
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
        var discountCard = cut.FindAll(".card").First(c => c.TextContent.Contains("Discount", StringComparison.Ordinal));
        TestAssert.Contains("Discount (Optional)", discountCard.QuerySelector(".card-title")!.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-percent", discountCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_discounttype_dropdown()
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
        TestAssert.Equal(3, options.Length);
        TestAssert.Contains("No Discount", options[0].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Percentage (0-100%)", options[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Absolute Amount", options[2].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_price_breakdown_when_tour_selected()
    {
        // Arrange
        var tour = BuildTourDto(price: 1000m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
        TestAssert.Contains("Price Breakdown", priceCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-calculator", priceCard.InnerHtml, StringComparison.Ordinal);
        TestAssert.Contains("Subtotal", priceCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Final Total", priceCard.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Price_breakdown_shows_discount_when_applied()
    {
        // Arrange
        var tour = BuildTourDto(price: 1000m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
        TestAssert.Contains("Discount", priceCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("(10.00%)", priceCard.TextContent, StringComparison.Ordinal);
        var discountRows = priceCard.QuerySelectorAll(".text-danger");
        TestAssert.NotEmpty(discountRows);
    }

    [Fact]
    public void Live_price_breakdown_should_recalculate_for_customer_bike_room_type_and_discount_changes()
    {
        // Arrange
        var tour = BuildTourDto(
            price: 1000m,
            currency: CurrencyDto.UsDollar,
            regularBikePrice: 50m,
            eBikePrice: 100m,
            singleRoomSupplementPrice: 200m);
        var principalCustomer = BuildCustomerDto(firstName: AliceFirstName, lastName: AliceLastName, email: AliceEmail, bikeType: BikeTypeDto.Regular);
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.DoubleOccupancy,
            PrincipalBikeType = BikeTypeDto.None
        };
        var tours = new List<GetTourDto> { tour };
        var customers = new List<GetCustomerDto> { principalCustomer };

        // Act
        var cut = Render<BookingCreateForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Tours, tours)
            .Add(p => p.Customers, [.. customers]));

        // Assert
        TestAssert.Contains("$ 1,000.00", cut.Markup, StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[1].Change(principalCustomer.Id);

        // Assert
        TestAssert.Contains("$ 1,050.00", cut.Markup, StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[3].Change(BikeTypeDto.EBike);

        // Assert
        TestAssert.Contains("$ 1,100.00", cut.Markup, StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[2].Change(RoomTypeDto.SingleOccupancy);

        // Assert
        TestAssert.Contains("$ 1,300.00", cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains("Final Total:", cut.Markup, StringComparison.Ordinal);

        // Act
        cut.Find("select#discountType").Change(DiscountTypeDto.Percentage);
        cut.Find("input#discountAmount").Change(10m);

        // Assert
        TestAssert.Contains("(10.00%)", cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains("-$ 130.00", cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains("$ 1,170.00", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Shows_warning_when_final_price_is_zero_or_negative()
    {
        // Arrange
        var tour = BuildTourDto(
            price: 100m,
            regularBikePrice: 0m,
            eBikePrice: 0m,
            singleRoomSupplementPrice: 0m);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
        var warning = priceCard.QuerySelector(".alert.alert-warning");
        _ = TestAssert.NotNull(warning);
        TestAssert.Contains("Final price cannot be zero or negative", warning.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-exclamation-triangle", warning.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Hides_price_breakdown_when_no_tour_selected()
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
        TestAssert.DoesNotContain(cards, c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_create_button()
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
        TestAssert.Contains("Create Booking", createButton.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-primary", createButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_cancel_button()
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
        TestAssert.Contains("btn-secondary", cancelButton.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_button_shows_spinner_when_submitting()
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
        _ = TestAssert.NotNull(spinner);
        TestAssert.Contains("spinner-border-sm", spinner.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Buttons_are_disabled_when_submitting()
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
        TestAssert.True(createButton.HasAttribute("disabled"));
        TestAssert.True(cancelButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task OnCancel_is_called_when_cancel_button_is_clicked()
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
        TestAssert.True(cancelCalled);
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
        _ = TestAssert.NotNull(validator);
    }

    [Fact]
    public void Displays_price_breakdown_with_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1500m, currency: CurrencyDto.Euro);
        var tours = new List<GetTourDto> { tour };
        var model = new BookingFormModel
        {
            TourId = tour.Id,
            RoomType = RoomTypeDto.DoubleOccupancy,
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
        var priceCard = priceCards.First(c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
        TestAssert.Contains("€", priceCard.TextContent, StringComparison.Ordinal);
    }
}
