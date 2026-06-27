using System.Globalization;
using ViajantesTurismo.Management.Web.Components.Pages.Tours;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

public class DetailsPageTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi;

    public DetailsPageTests()
    {
        _fakeToursApi = new FakeToursApiClient();

        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
        Services.AddSingleton<IBookingsApiClient>(new FakeBookingsApiClient());
        Services.AddSingleton<ICustomersApiClient>(new FakeCustomersApiClient());
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

        cut.WaitForAssertion(() => Assert.Contains(tour.Name, cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains(tour.Identifier, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(tour.Name, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(tour.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(tour.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(tour.Currency.ToString(), cut.Markup, StringComparison.Ordinal);
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

        cut.WaitForAssertion(() => Assert.Contains($"{expectedDuration} days", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains($"{expectedDuration} days", cut.Markup, StringComparison.Ordinal);
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

        cut.WaitForAssertion(() => Assert.Contains("R$ 1,500.00", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains("R$ 1,500.00", cut.Markup, StringComparison.Ordinal); // Base Price
        Assert.Contains("R$ 300.00", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
        Assert.Contains("R$ 100.00", cut.Markup, StringComparison.Ordinal); // Regular Bike
        Assert.Contains("R$ 250.00", cut.Markup, StringComparison.Ordinal); // E-Bike
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

        cut.WaitForAssertion(() => Assert.Contains("1,500.00 €", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains("1,500.00 €", cut.Markup, StringComparison.Ordinal); // Base Price
        Assert.Contains("300.00 €", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
    }

    [Fact]
    public void Renders_pricing_information_with_usDollar_currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.UsDollar);

        TourDetailsPageTestsHelper.SetupSuccessfulTourLoad(_fakeToursApi, tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains("$ 1,500.00", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains("$ 1,500.00", cut.Markup, StringComparison.Ordinal); // Base Price
        Assert.Contains("$ 300.00", cut.Markup, StringComparison.Ordinal); // Single Room Supplement
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

        cut.WaitForAssertion(() => Assert.Contains($"{tour.MinCustomers}", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains($"{tour.MinCustomers}", cut.Markup, StringComparison.Ordinal);
        Assert.Contains($"{tour.MaxCustomers}", cut.Markup, StringComparison.Ordinal);
        Assert.Contains($"{tour.CurrentCustomerCount} / {tour.MaxCustomers} customers", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("15 spots available", badge.TextContent, StringComparison.Ordinal);
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
        Assert.Contains("Fully Booked", badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_below_minimum_badge_when_under_minCustomers()
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
        Assert.Contains("Below Minimum", badge.TextContent, StringComparison.Ordinal);
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

        cut.WaitForAssertion(() => Assert.Contains("Included Services", cut.Markup, StringComparison.Ordinal));

        // Assert
        Assert.Contains("Included Services", cut.Markup, StringComparison.Ordinal);
        var serviceItems = cut.FindAll("ul.list-group > li.list-group-item");
        Assert.Equal(4, serviceItems.Count);
        Assert.Contains("Breakfast", serviceItems[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Lunch", serviceItems[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("Bike rental", serviceItems[2].TextContent, StringComparison.Ordinal);
        Assert.Contains("Tour guide", serviceItems[3].TextContent, StringComparison.Ordinal);
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
        Assert.DoesNotContain("Included Services", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("ul.list-group"));
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
        Assert.Equal($"/edittour/{tour.Id}", editLink.GetAttribute("href"));
        Assert.Contains("Edit Tour", editLink.TextContent, StringComparison.Ordinal);
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
        Assert.Contains(backLinks, link => link.GetAttribute("href") == "/tours");
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
        Assert.Equal("Tour Details", pageTitle.TextContent);
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
        Assert.Equal("Amazing Bike Tour 2024", cardHeader.TextContent);
    }

}
