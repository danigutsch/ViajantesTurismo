using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using PandaBear.Testing;
using Index = ViajantesTurismo.Management.Web.Components.Pages.Tours.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

[TestScope(TestScope.Component)]
public class IndexPageTests : BunitContext
{
    private readonly FakeToursApiClient _fakeToursApi;

    public IndexPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _fakeToursApi = new FakeToursApiClient();
        Services.AddSingleton<IToursApiClient>(_fakeToursApi);
    }

    [Fact]
    public void Renders_tour_basic_information()
    {
        // Arrange
        var tour = BuildTourDto(
            identifier: "TOUR-2024-001",
            name: "Amazing Bike Tour",
            startDate: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            endDate: new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Unspecified)
        );
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("TOUR-2024-001", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("TOUR-2024-001", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Amazing Bike Tour", StringComparison.Ordinal);
        cut.Markup.ShouldContain("2024", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_real_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1500m, currency: "BRL");
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("R$", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain(MoneyText.Of(1500.00m, "BRL"), StringComparison.Ordinal);
        cut.Markup.ShouldContain(MoneyText.Of(300.00m, "BRL"), StringComparison.Ordinal);
        cut.Markup.ShouldContain(MoneyText.Of(100.00m, "BRL"), StringComparison.Ordinal);
        cut.Markup.ShouldContain(MoneyText.Of(250.00m, "BRL"), StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_euro_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1200m, currency: "EUR");
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains('€', StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain(MoneyText.Of(1200.00m, "EUR"), StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_usdollar_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1800m, currency: "USD");
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains(MoneyText.Of(1800.00m, "USD"), StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain(MoneyText.Of(1800.00m, "USD"), StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_available_spots_badge_when_tour_has_capacity()
    {
        // Arrange
        var tour = WithCapacityFacts(BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 15
        });
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-success").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-success");
        badge.TextContent.ShouldContain("15 spots", StringComparison.Ordinal);
        cut.Markup.ShouldContain("15 / 30", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_fully_booked_badge_when_at_max_capacity()
    {
        // Arrange
        var tour = WithCapacityFacts(BuildTourDto() with
        {
            MaxCustomers = 30,
            CurrentCustomerCount = 30
        });
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-danger").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-danger");
        badge.TextContent.ShouldContain("Full", StringComparison.Ordinal);
        cut.Markup.ShouldContain("30 / 30", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_below_minimum_badge_when_under_mincustomers()
    {
        // Arrange
        var tour = WithCapacityFacts(BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 5
        });
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-warning").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-warning");
        badge.TextContent.ShouldContain("Below Min", StringComparison.Ordinal);
        cut.Markup.ShouldContain("5 / 30", StringComparison.Ordinal);
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Renders_available_spots_from_the_read_model_fact()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 15,
            AvailableSpots = 7,
            IsFullyBooked = false,
            IsAtMinimumCapacity = true
        };
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-success").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-success");
        badge.TextContent.ShouldContain("7 spots", StringComparison.Ordinal);
        badge.GetAttribute("title").ShouldBe("7 spots available");
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Renders_fully_booked_badge_from_the_read_model_fact()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 15,
            AvailableSpots = 0,
            IsFullyBooked = true,
            IsAtMinimumCapacity = true
        };
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-danger").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-danger");
        badge.TextContent.ShouldContain("Full", StringComparison.Ordinal);
        badge.GetAttribute("title").ShouldBe("Tour is fully booked");
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Renders_below_minimum_badge_from_the_read_model_fact()
    {
        // Arrange
        var tour = BuildTourDto() with
        {
            MinCustomers = 10,
            MaxCustomers = 30,
            CurrentCustomerCount = 15,
            AvailableSpots = 15,
            IsFullyBooked = false,
            IsAtMinimumCapacity = false
        };
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-warning").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-warning");
        badge.TextContent.ShouldContain("Below Min", StringComparison.Ordinal);
    }

    [Fact]
    public void Sorting_by_name_sets_an_ascending_aria_state()
    {
        // Arrange
        _fakeToursApi.AddTour(BuildTourDto(identifier: "TOUR-A", name: "Alpha Tour"));
        _fakeToursApi.AddTour(BuildTourDto(identifier: "TOUR-Z", name: "Zulu Tour"));
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Alpha Tour", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Act
        var nameSortLink = cut.FindAll("a.col-title").ShouldHaveSingleItem(link => link.TextContent.Trim() == "Name");
        // QuickGrid 11 renders a sortable header as a link to the sorted URL, which the browser follows.
        Services.GetRequiredService<NavigationManager>().NavigateTo(nameSortLink.GetAttribute("href").ShouldNotBeNull());

        // Assert
        IndexPageTestsHelper.AssertNameSortIsAscending(cut);
        var lastRequest = _fakeToursApi.TourPageRequests[^1];
        lastRequest.ShouldBe((1, TourPageSort.Name, false));
        _fakeToursApi.GetToursCallCount.ShouldBe(0);
    }

    [Fact]
    public void Transport_failure_while_loading_shows_a_safe_error_message()
    {
        // Arrange
        _fakeToursApi.SetGetTourPageOutcome(FakeApiClientOutcomes.TransportFailure<GetTourPageDto>());

        // Act
        var cut = Render<Index>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("Tours could not be loaded. Try again later."));
    }

    [Theory]
    [InlineData("Identifier", TourPageSort.Identifier)]
    [InlineData("Name", TourPageSort.Name)]
    [InlineData("Start Date", TourPageSort.StartDate)]
    [InlineData("End Date", TourPageSort.EndDate)]
    [InlineData("Price", TourPageSort.Price)]
    [InlineData("Capacity", TourPageSort.CurrentCustomerCount)]
    public async Task Large_tour_grid_pages_and_sorts_using_the_bounded_route(string title, TourPageSort sort)
    {
        // Arrange
        foreach (var index in Enumerable.Range(0, 125))
        {
            _fakeToursApi.AddTour(BuildTourDto(id: Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}"),
                identifier: $"TOUR-{index:D3}", name: $"Fixture{index:D3}"));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fixture115", StringComparison.Ordinal));
        var initialMarkup = cut.Markup;
        var initialRequest = _fakeToursApi.TourPageRequests[^1];
        var pagination = cut.FindComponent<Paginator>().Instance.State;
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(10));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Fixture015", StringComparison.Ordinal));
        var laterMarkup = cut.Markup;
        var laterRequest = _fakeToursApi.TourPageRequests[^1];
        ColumnBase<GetTourListItemDto> column = sort switch
        {
            TourPageSort.Identifier or TourPageSort.Name => cut.FindComponents<PropertyColumn<GetTourListItemDto, string>>()
                .ShouldHaveSingleItem(item => item.Instance.Title == title).Instance,
            TourPageSort.StartDate or TourPageSort.EndDate => cut.FindComponents<PropertyColumn<GetTourListItemDto, DateOnly>>()
                .ShouldHaveSingleItem(item => item.Instance.Title == title).Instance,
            _ => cut.FindComponents<TemplateColumn<GetTourListItemDto>>()
                .ShouldHaveSingleItem(item => item.Instance.Title == title).Instance
        };
        var grid = cut.FindComponent<QuickGrid<GetTourListItemDto>>().Instance;
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Descending));
        var (_, descendingSort, descending) = _fakeToursApi.TourPageRequests[^1];
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Ascending));
        var (_, ascendingSort, ascendingDescending) = _fakeToursApi.TourPageRequests[^1];

        // Assert
        initialMarkup.ShouldContain("Total tours: 125", StringComparison.Ordinal);
        initialMarkup.ShouldNotContain("Fixture114", StringComparison.Ordinal);
        laterMarkup.ShouldNotContain("Fixture014", StringComparison.Ordinal);
        initialRequest.ShouldBe((1, TourPageSort.Default, false));
        laterRequest.ShouldBe((11, TourPageSort.Default, false));
        descendingSort.ShouldBe(sort);
        descending.ShouldBeTrue();
        ascendingSort.ShouldBe(sort);
        ascendingDescending.ShouldBeFalse();
        _fakeToursApi.GetToursCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_later_page_failure_clears_the_stale_tour_rows_and_shows_the_safe_error()
    {
        // Arrange
        foreach (var index in Enumerable.Range(0, 15))
        {
            _fakeToursApi.AddTour(BuildTourDto(identifier: $"TOUR-{index:D3}", name: $"Fixture{index:D3}"));
        }

        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Total tours: 15", StringComparison.Ordinal));
        var pagination = cut.FindComponent<Paginator>().Instance.State;
        _fakeToursApi.SetGetTourPageOutcome(FakeApiClientOutcomes.TransportFailure<GetTourPageDto>());

        // Act
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(1));

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("Tours could not be loaded. Try again later."));
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }
}
