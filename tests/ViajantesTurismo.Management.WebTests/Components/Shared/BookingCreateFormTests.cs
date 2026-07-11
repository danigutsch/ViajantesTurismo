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
        (options.Length).ShouldBe(3); // Placeholder + 2 tours
        (options[1].TextContent).ShouldContain("Tour A (01/06/2025)", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Tour B (01/07/2025)", StringComparison.Ordinal);
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
        (availabilityText.TextContent).ShouldContain("7 spots available", StringComparison.Ordinal);
        (availabilityText.TextContent).ShouldContain("3 / 10 booked", StringComparison.Ordinal);
        var successSpan = availabilityText.QuerySelector(".text-success");
        _ = (successSpan).ShouldNotBeNull();
        (successSpan.InnerHtml).ShouldContain("bi-check-circle", StringComparison.Ordinal);
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
        (availabilityText.TextContent).ShouldContain("Tour is fully booked", StringComparison.Ordinal);
        (availabilityText.TextContent).ShouldContain("10 / 10", StringComparison.Ordinal);
        var dangerSpan = availabilityText.QuerySelector(".text-danger");
        _ = (dangerSpan).ShouldNotBeNull();
        (dangerSpan.InnerHtml).ShouldContain("bi-x-circle", StringComparison.Ordinal);
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
        (availabilityText.TextContent).ShouldContain("1 spot available", StringComparison.Ordinal);
        (availabilityText.TextContent).ShouldNotContain("spots", StringComparison.Ordinal);
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
        (options.Length).ShouldBe(3);
        (options[1].TextContent).ShouldContain(AliceCustomerDisplayName, StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Bob Smith (bob@example.com)", StringComparison.Ordinal);
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
        (options.Length).ShouldBe(2);
        (options[0].TextContent).ShouldContain("Double Room (Base Price)", StringComparison.Ordinal);
        (options[1].TextContent).ShouldContain("Single Room (Base Price + Supplement)", StringComparison.Ordinal);
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
        (badge.TextContent).ShouldContain("Single Occupancy", StringComparison.Ordinal);
        (badge.TextContent).ShouldContain("No companion selected", StringComparison.Ordinal);
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
        (options.Length).ShouldBe(3); // Placeholder + 2 bike types
        (options[1].TextContent).ShouldContain("Regular Bike", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("E-Bike", StringComparison.Ordinal);
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
        (labels).ShouldNotContain(l => l.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));
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
        _ = (companionLabel).ShouldNotBeNull();

        var companionSelect = cut.FindAll(SelectSelector)
            .First(s => s.TextContent.Contains("-- No Companion --", StringComparison.Ordinal));
        var options = companionSelect.QuerySelectorAll("option");
        (options.Length).ShouldBe(2); // "No Companion" + Bob (Alice excluded)
        (options).ShouldNotContain(o => o.TextContent.Contains(AliceDisplayName, StringComparison.Ordinal));
        (options).ShouldContain(o => o.TextContent.Contains("Bob Smith", StringComparison.Ordinal));
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
        (labels).ShouldContain(l => l.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
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
        (selectsAfterCompanionSelection[2].GetAttribute(ValueAttributeName)).ShouldBe("EBike");
        (selectsAfterCompanionSelection[4].GetAttribute(ValueAttributeName)).ShouldBe("Regular");

        // Act
        selectsAfterCompanionSelection[1].Change(RoomTypeDto.SingleOccupancy);

        // Assert
        var labelsAfterSingleRoom = cut.FindAll(LabelSelector);
        (labelsAfterSingleRoom).ShouldNotContain(label => label.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));
        (labelsAfterSingleRoom).ShouldNotContain(label => label.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
        (cut.Find(".badge.bg-info").TextContent).ShouldContain("Single Occupancy", StringComparison.Ordinal);
        (model.CompanionId).ShouldBeNull();
        (model.CompanionBikeType).ShouldBeNull();

        // Act
        cut.FindAll(SelectSelector)[1].Change(RoomTypeDto.DoubleOccupancy);

        // Assert
        var labelsAfterReturningToDouble = cut.FindAll(LabelSelector);
        (labelsAfterReturningToDouble).ShouldContain(label => label.TextContent.Contains(CompanionOptionalLabel, StringComparison.Ordinal));

        var companionSelect = cut.FindAll(SelectSelector)[3];
        (companionSelect.GetAttribute(ValueAttributeName) ?? string.Empty).ShouldBe(string.Empty);
        (cut.FindAll(LabelSelector)).ShouldNotContain(label => label.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
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
        (labels).ShouldNotContain(l => l.TextContent.Contains(CompanionBikeLabel, StringComparison.Ordinal));
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
        (notesTextArea.GetAttribute(ValueAttributeName)).ShouldBe("Special request");
        (notesTextArea.GetAttribute("rows")).ShouldBe("3");
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
        (discountCard.QuerySelector(".card-title")!.TextContent).ShouldContain("Discount (Optional)", StringComparison.Ordinal);
        (discountCard.InnerHtml).ShouldContain("bi-percent", StringComparison.Ordinal);
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
        (options.Length).ShouldBe(3);
        (options[0].TextContent).ShouldContain("No Discount", StringComparison.Ordinal);
        (options[1].TextContent).ShouldContain("Percentage (0-100%)", StringComparison.Ordinal);
        (options[2].TextContent).ShouldContain("Absolute Amount", StringComparison.Ordinal);
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
        (priceCard.TextContent).ShouldContain("Price Breakdown", StringComparison.Ordinal);
        (priceCard.InnerHtml).ShouldContain("bi-calculator", StringComparison.Ordinal);
        (priceCard.TextContent).ShouldContain("Subtotal", StringComparison.Ordinal);
        (priceCard.TextContent).ShouldContain("Final Total", StringComparison.Ordinal);
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
        (priceCard.TextContent).ShouldContain("Discount", StringComparison.Ordinal);
        (priceCard.TextContent).ShouldContain("(10.00%)", StringComparison.Ordinal);
        var discountRows = priceCard.QuerySelectorAll(".text-danger");
        (discountRows).ShouldNotBeEmpty();
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
        (cut.Markup).ShouldContain("$ 1,000.00", StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[1].Change(principalCustomer.Id);

        // Assert
        (cut.Markup).ShouldContain("$ 1,050.00", StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[3].Change(BikeTypeDto.EBike);

        // Assert
        (cut.Markup).ShouldContain("$ 1,100.00", StringComparison.Ordinal);

        // Act
        cut.FindAll(SelectSelector)[2].Change(RoomTypeDto.SingleOccupancy);

        // Assert
        (cut.Markup).ShouldContain("$ 1,300.00", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Final Total:", StringComparison.Ordinal);

        // Act
        cut.Find("select#discountType").Change(DiscountTypeDto.Percentage);
        cut.Find("input#discountAmount").Change(10m);

        // Assert
        (cut.Markup).ShouldContain("(10.00%)", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("-$ 130.00", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("$ 1,170.00", StringComparison.Ordinal);
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
        _ = (warning).ShouldNotBeNull();
        (warning.TextContent).ShouldContain("Final price cannot be zero or negative", StringComparison.Ordinal);
        (warning.InnerHtml).ShouldContain("bi-exclamation-triangle", StringComparison.Ordinal);
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
        (cards).ShouldNotContain(c => c.TextContent.Contains("Price Breakdown", StringComparison.Ordinal));
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
        (createButton.TextContent).ShouldContain("Create Booking", StringComparison.Ordinal);
        (createButton.ClassName).ShouldContain("btn-primary", StringComparison.Ordinal);
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
        (cancelButton.ClassName).ShouldContain("btn-secondary", StringComparison.Ordinal);
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
        _ = (spinner).ShouldNotBeNull();
        (spinner.ClassName).ShouldContain("spinner-border-sm", StringComparison.Ordinal);
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
        (createButton.HasAttribute("disabled")).ShouldBeTrue();
        (cancelButton.HasAttribute("disabled")).ShouldBeTrue();
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
        (cancelCalled).ShouldBeTrue();
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
        _ = (validator).ShouldNotBeNull();
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
        (priceCard.TextContent).ShouldContain("€", StringComparison.Ordinal);
    }
}
