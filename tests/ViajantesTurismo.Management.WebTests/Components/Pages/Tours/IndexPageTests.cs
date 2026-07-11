using Index = ViajantesTurismo.Management.Web.Components.Pages.Tours.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Tours;

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
        (cut.Markup).ShouldContain("TOUR-2024-001", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Amazing Bike Tour", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("2024", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_real_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1500m, currency: CurrencyDto.Real);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("R$", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        (cut.Markup).ShouldContain("R$ 1,500.00", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("R$ 300.00", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("R$ 100.00", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("R$ 250.00", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_euro_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1200m, currency: CurrencyDto.Euro);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains('€', StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        (cut.Markup).ShouldContain("1,200.00 €", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_price_with_usdollar_currency_symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1800m, currency: CurrencyDto.UsDollar);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("$ 1,800.00", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        (cut.Markup).ShouldContain("$ 1,800.00", StringComparison.Ordinal);
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
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-success").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-success");
        (badge.TextContent).ShouldContain("15 spots", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("15 / 30", StringComparison.Ordinal);
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
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-danger").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-danger");
        (badge.TextContent).ShouldContain("Full", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("30 / 30", StringComparison.Ordinal);
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
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("span.badge.bg-warning").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var badge = cut.Find("span.badge.bg-warning");
        (badge.TextContent).ShouldContain("Below Min", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("5 / 30", StringComparison.Ordinal);
    }
}
