using Index = ViajantesTurismo.Admin.Web.Components.Pages.Tours.Index;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Tours;

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
    public void Renders_Tour_Basic_Information()
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
        Assert.Contains("TOUR-2024-001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Amazing Bike Tour", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("2024", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Price_With_Real_Currency_Symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1500m, currency: CurrencyDto.Real);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("R$", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("R$ 1,500.00", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("R$ 300.00", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("R$ 100.00", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("R$ 250.00", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Price_With_Euro_Currency_Symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1200m, currency: CurrencyDto.Euro);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains('€', StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("1,200.00 €", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Price_With_UsDollar_Currency_Symbol()
    {
        // Arrange
        var tour = BuildTourDto(price: 1800m, currency: CurrencyDto.UsDollar);
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("$ 1,800.00", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("$ 1,800.00", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Available_Spots_Badge_When_Tour_Has_Capacity()
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
        Assert.Contains("15 spots", badge.TextContent, StringComparison.Ordinal);
        Assert.Contains("15 / 30", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Fully_Booked_Badge_When_At_Max_Capacity()
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
        Assert.Contains("Full", badge.TextContent, StringComparison.Ordinal);
        Assert.Contains("30 / 30", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Below_Minimum_Badge_When_Under_MinCustomers()
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
        Assert.Contains("Below Min", badge.TextContent, StringComparison.Ordinal);
        Assert.Contains("5 / 30", cut.Markup, StringComparison.Ordinal);
    }
}
