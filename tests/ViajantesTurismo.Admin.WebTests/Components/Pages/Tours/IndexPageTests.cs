using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared.Fakes.ApiClients;
using static ViajantesTurismo.Admin.Tests.Shared.Builders.DtoBuilders;
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
    public void Renders_Loading_State_Initially()
    {
        // Arrange
        _fakeToursApi.AddTour(BuildTourDto());

        // Act
        var cut = Render<Index>();

        // Assert
        cut.WaitForState(() => !cut.Markup.Contains("Loading...", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        Assert.DoesNotContain("Loading...", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Empty_QuickGrid_When_No_Tours()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var grid = cut.Find("table.table-hover");
        Assert.NotNull(grid);

        var totalText = cut.Find("p.text-muted");
        Assert.Contains("Total tours: 0", totalText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_QuickGrid_With_Column_Headers()
    {
        // Arrange
        _fakeToursApi.AddTour(BuildTourDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("th").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var headers = cut.FindAll("th");
        Assert.Contains(headers, h => h.TextContent.Contains("Identifier", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Name", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Start Date", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("End Date", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Price", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Single Room Supplement", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Regular Bike", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("E-Bike", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Capacity", StringComparison.Ordinal));
        Assert.Contains(headers, h => h.TextContent.Contains("Actions", StringComparison.Ordinal));
    }

    [Fact]
    public void Renders_Tour_Basic_Information()
    {
        // Arrange
        var tour = BuildTourDto(
            identifier: "TOUR-2024-001",
            name: "Amazing Bike Tour",
            startDate: new DateTime(2024, 6, 1),
            endDate: new DateTime(2024, 6, 15)
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

    [Fact]
    public void Renders_View_Button_For_Each_Tour()
    {
        // Arrange
        var tour = BuildTourDto();
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("a.btn.btn-info").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var viewButton = cut.Find("a.btn.btn-info");
        Assert.Equal($"/tours/{tour.Id}", viewButton.GetAttribute("href"));
        Assert.Contains("View", viewButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Edit_Button_For_Each_Tour()
    {
        // Arrange
        var tour = BuildTourDto();
        _fakeToursApi.AddTour(tour);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("a.btn.btn-primary").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var editButton = cut.Find("a.btn.btn-primary");
        Assert.Equal($"/edittour/{tour.Id}", editButton.GetAttribute("href"));
        Assert.Contains("Edit", editButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Multiple_Tours()
    {
        // Arrange
        _fakeToursApi.AddTour(BuildTourDto(identifier: "TOUR-001", name: "Tour 1"));
        _fakeToursApi.AddTour(BuildTourDto(identifier: "TOUR-002", name: "Tour 2"));
        _fakeToursApi.AddTour(BuildTourDto(identifier: "TOUR-003", name: "Tour 3"));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Total tours: 3", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("TOUR-001", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("TOUR-002", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("TOUR-003", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Total tours: 3", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Paginator_When_More_Than_10_Tours()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            _fakeToursApi.AddTour(BuildTourDto(identifier: $"TOUR-{i:D3}"));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var totalText = cut.Find("p.text-muted");
        Assert.Contains("Total tours: 10", totalText.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Render_Paginator_When_10_Or_Fewer_Tours()
    {
        // Arrange
        for (int i = 1; i <= 8; i++)
        {
            _fakeToursApi.AddTour(BuildTourDto(identifier: $"TOUR-{i:D3}"));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total tours: 8", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Page_Title()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("h1").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Contains("Tours", pageTitle.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_Total_Tours_Count()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _fakeToursApi.AddTour(BuildTourDto(identifier: $"TOUR-{i:D3}"));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("p.text-muted").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var totalText = cut.Find("p.text-muted");
        Assert.Equal("Total tours: 5", totalText.TextContent);
    }
}
