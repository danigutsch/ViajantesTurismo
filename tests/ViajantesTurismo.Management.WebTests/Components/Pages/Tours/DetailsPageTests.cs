using System.Globalization;
using System.Net;
using SharedKernel.HttpClients;
using ViajantesTurismo.Management.Web.Components.Pages.Tours;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

public class DetailsPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi;
    private readonly FakeCustomersApiClient _fakeCustomersApi;
    private readonly FakeToursApiClient _fakeToursApi;

    public DetailsPageTests()
    {
        _fakeBookingsApi = new FakeBookingsApiClient();
        _fakeCustomersApi = new FakeCustomersApiClient();
        _fakeToursApi = new FakeToursApiClient();

        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
    }

    [Fact]
    public void Renders_tour_details_with_general_information()
    {
        // Arrange
        var tour = BuildTourDto();

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains(tour.Name, cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains(tour.Identifier, cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains(tour.Name, cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains(tour.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains(tour.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains(tour.Currency.ToString(), cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_tour_duration_in_days()
    {
        // Arrange
        var tour = BuildTourDto();
        var expectedDuration = (tour.EndDate - tour.StartDate).Days;

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains($"{expectedDuration} days", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains($"{expectedDuration} days", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_pricing_information_with_real_currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.Real);

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains("R$ 1,500.00", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains("R$ 1,500.00", cut.Markup, StringComparison.Ordinal); // Base Price
        TestAssert.Contains("R$ 300.00", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
        TestAssert.Contains("R$ 100.00", cut.Markup, StringComparison.Ordinal); // Regular Bike
        TestAssert.Contains("R$ 250.00", cut.Markup, StringComparison.Ordinal); // E-Bike
    }

    [Fact]
    public void Renders_pricing_information_with_euro_currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.Euro);

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains("1,500.00 €", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains("1,500.00 €", cut.Markup, StringComparison.Ordinal); // Base Price
        TestAssert.Contains("300.00 €", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
    }

    [Fact]
    public void Renders_pricing_information_with_usdollar_currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.UsDollar);

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains("$ 1,500.00", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains("$ 1,500.00", cut.Markup, StringComparison.Ordinal); // Base Price
        TestAssert.Contains("$ 300.00", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
    }

    [Fact]
    public void Renders_capacity_information()
    {
        // Arrange
        var tour = BuildTourDto();

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains($"{tour.MinCustomers}", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains($"{tour.MinCustomers}", cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains($"{tour.MaxCustomers}", cut.Markup, StringComparison.Ordinal);
        TestAssert.Contains($"{tour.CurrentCustomerCount} / {tour.MaxCustomers} customers", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_available_spots_badge_when_tour_has_capacity()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 15
        };

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-success"));

        // Assert
        var badge = cut.Find("span.badge.bg-success");
        TestAssert.Contains("15 spots available", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Create_booking_non_success_outcome_shows_sanitized_error()
    {
        // Arrange
        var tour = BuildTourDto();
        var customer = BuildCustomerDto();
        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);
        _fakeCustomersApi.AddCustomer(customer);
        _fakeBookingsApi.SetCreateBookingOutcome(ContractCommandOutcome.Status(ContractCommandOutcomeKind.Conflict, HttpStatusCode.Conflict));

        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, tour.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("button:contains('Add Booking')"));

        // Act
        await cut.InvokeAsync(() => cut.Find("button:contains('Add Booking')").Click());
        await cut.WaitForAssertionAsync(() => cut.FindComponent<BookingCreateForm>());

        var form = cut.FindComponent<BookingCreateForm>();
        form.Instance.Model.CustomerId = customer.Id;
        form.Instance.Model.PrincipalBikeType = BikeTypeDto.Regular;
        await cut.InvokeAsync(() => form.Find("form").Submit());

        // Assert
        await cut.WaitForAssertionAsync(() =>
        {
            cut.Markup.ShouldContain("Failed to create booking. Please try again.", StringComparison.Ordinal);
            cut.Markup.ShouldContain("Create New Booking", StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Renders_fully_booked_badge_when_at_max_capacity()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MaxCustomers = 30,
            CurrentCustomerCount = 30
        };

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-danger"));

        // Assert
        var badge = cut.Find("span.badge.bg-danger");
        TestAssert.Contains("Fully Booked", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_below_minimum_badge_when_under_mincustomers()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 5
        };

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-warning"));

        // Assert
        var badge = cut.Find("span.badge.bg-warning");
        TestAssert.Contains("Below Minimum", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_included_services_when_available()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            IncludedServices = new List<string>
            {
                "Breakfast",
                "Lunch",
                "Bike rental",
                "Tour guide"
            }
        };

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => TestAssert.Contains("Included Services", cut.Markup, StringComparison.Ordinal));

        // Assert
        TestAssert.Contains("Included Services", cut.Markup, StringComparison.Ordinal);
        var serviceItems = cut.FindAll("ul.list-group > li.list-group-item");
        TestAssert.Equal(4, serviceItems.Count);
        TestAssert.Contains("Breakfast", serviceItems[0].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Lunch", serviceItems[1].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Bike rental", serviceItems[2].TextContent, StringComparison.Ordinal);
        TestAssert.Contains("Tour guide", serviceItems[3].TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_not_render_services_section_when_empty()
    {
        // Arrange
        var tour = BuildTourDto(
            includedServices: new List<string>()
        );

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find(".card-header h3"));

        // Assert
        TestAssert.DoesNotContain("Included Services", cut.Markup, StringComparison.Ordinal);
        TestAssert.Empty(cut.FindAll("ul.list-group"));
    }

    [Fact]
    public void Renders_edit_tour_link()
    {
        // Arrange
        var tour = BuildTourDto();

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("a.btn.btn-primary"));

        // Assert
        var editLink = cut.Find("a.btn.btn-primary");
        TestAssert.Equal($"/edittour/{tour.Id}", editLink.GetAttribute("href"));
        TestAssert.Contains("Edit Tour", editLink.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_back_to_list_link()
    {
        // Arrange
        var tour = BuildTourDto();

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.FindAll("a.btn.btn-secondary"));

        // Assert
        var backLinks = cut.FindAll("a.btn.btn-secondary");
        TestAssert.Contains(backLinks, link => link.GetAttribute("href") == "/tours");
    }

    [Fact]
    public void Renders_page_title()
    {
        // Arrange
        var tour = BuildTourDto();

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        var pageTitle = cut.Find("h1");
        TestAssert.Equal("Tour Details", pageTitle.TextContent);
    }

    [Fact]
    public void Renders_tour_name_in_card_header()
    {
        // Arrange
        var tour = BuildTourDto(name: "Amazing Bike Tour 2024");

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find(".card-header h3"));

        // Assert
        var cardHeader = cut.Find(".card-header h3");
        TestAssert.Equal("Amazing Bike Tour 2024", cardHeader.TextContent);
    }

}
