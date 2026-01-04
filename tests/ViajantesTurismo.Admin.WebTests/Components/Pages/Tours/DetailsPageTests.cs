using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Tours;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Tours;

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
    public void Renders_NotFound_When_Tour_Is_Null()
    {
        // Arrange
        var tourId = Guid.NewGuid();

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tourId));

        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Tour not found", alert.TextContent);

        var backLink = cut.Find("a.btn.btn-secondary");
        Assert.Equal("/tours", backLink.GetAttribute("href"));
    }

    [Fact]
    public void Renders_NotFound_When_API_Throws_Exception()
    {
        // Arrange
        var tourId = Guid.NewGuid();

        _fakeToursApi.SetGetTourByIdException(new HttpRequestException("Not found"));

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tourId));

        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Tour not found", alert.TextContent);
    }

    [Fact]
    public void Renders_Tour_Details_With_General_Information()
    {
        // Arrange
        var tour = BuildTourDto();

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains(tour.Name, cut.Markup));

        // Assert
        Assert.Contains(tour.Identifier, cut.Markup);
        Assert.Contains(tour.Name, cut.Markup);
        Assert.Contains(tour.StartDate.ToShortDateString(), cut.Markup);
        Assert.Contains(tour.EndDate.ToShortDateString(), cut.Markup);
        Assert.Contains(tour.Currency.ToString(), cut.Markup);
    }

    [Fact]
    public void Renders_Tour_Duration_In_Days()
    {
        // Arrange
        var tour = BuildTourDto();
        var expectedDuration = (tour.EndDate - tour.StartDate).Days;

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains($"{expectedDuration} days", cut.Markup));

        // Assert
        Assert.Contains($"{expectedDuration} days", cut.Markup);
    }

    [Fact]
    public void Renders_Pricing_Information_With_Real_Currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.Real);

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains("R$ 1,500.00", cut.Markup));

        // Assert
        Assert.Contains("R$ 1,500.00", cut.Markup); // Base Price
        Assert.Contains("R$ 300.00", cut.Markup); // Double Room Supplement
        Assert.Contains("R$ 100.00", cut.Markup); // Regular Bike
        Assert.Contains("R$ 250.00", cut.Markup); // E-Bike
    }

    [Fact]
    public void Renders_Pricing_Information_With_Euro_Currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.Euro);

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains("1,500.00 €", cut.Markup));

        // Assert
        Assert.Contains("1,500.00 €", cut.Markup); // Base Price
        Assert.Contains("300.00 €", cut.Markup); // Double Room Supplement
    }

    [Fact]
    public void Renders_Pricing_Information_With_UsDollar_Currency()
    {
        // Arrange
        var tour = BuildTourDto(currency: CurrencyDto.UsDollar);

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains("$ 1,500.00", cut.Markup));

        // Assert
        Assert.Contains("$ 1,500.00", cut.Markup); // Base Price
        Assert.Contains("$ 300.00", cut.Markup); // Double Room Supplement
    }

    [Fact]
    public void Renders_Capacity_Information()
    {
        // Arrange
        var tour = BuildTourDto();

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains($"{tour.MinCustomers}", cut.Markup));

        // Assert
        Assert.Contains($"{tour.MinCustomers}", cut.Markup);
        Assert.Contains($"{tour.MaxCustomers}", cut.Markup);
        Assert.Contains($"{tour.CurrentCustomerCount} / {tour.MaxCustomers} customers", cut.Markup);
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

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-success"));

        // Assert
        var badge = cut.Find("span.badge.bg-success");
        Assert.Contains("15 spots available", badge.TextContent);
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

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-danger"));

        // Assert
        var badge = cut.Find("span.badge.bg-danger");
        Assert.Contains("Fully Booked", badge.TextContent);
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

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("span.badge.bg-warning"));

        // Assert
        var badge = cut.Find("span.badge.bg-warning");
        Assert.Contains("Below Minimum", badge.TextContent);
    }

    [Fact]
    public void Renders_Included_Services_When_Available()
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

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => Assert.Contains("Included Services", cut.Markup));

        // Assert
        Assert.Contains("Included Services", cut.Markup);
        var serviceItems = cut.FindAll("ul.list-group > li.list-group-item");
        Assert.Equal(4, serviceItems.Count);
        Assert.Contains("Breakfast", serviceItems[0].TextContent);
        Assert.Contains("Lunch", serviceItems[1].TextContent);
        Assert.Contains("Bike rental", serviceItems[2].TextContent);
        Assert.Contains("Tour guide", serviceItems[3].TextContent);
    }

    [Fact]
    public void Does_Not_Render_Services_Section_When_Empty()
    {
        // Arrange
        var tour = BuildTourDto(
            includedServices: new List<string>()
        );

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find(".card-header h3"));

        // Assert
        Assert.DoesNotContain("Included Services", cut.Markup);
        Assert.Empty(cut.FindAll("ul.list-group"));
    }

    [Fact]
    public void Renders_Edit_Tour_Link()
    {
        // Arrange
        var tour = BuildTourDto();

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("a.btn.btn-primary"));

        // Assert
        var editLink = cut.Find("a.btn.btn-primary");
        Assert.Equal($"/edittour/{tour.Id}", editLink.GetAttribute("href"));
        Assert.Contains("Edit Tour", editLink.TextContent);
    }

    [Fact]
    public void Renders_Back_To_List_Link()
    {
        // Arrange
        var tour = BuildTourDto();

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.FindAll("a.btn.btn-secondary"));

        // Assert
        var backLinks = cut.FindAll("a.btn.btn-secondary");
        Assert.Contains(backLinks, link => link.GetAttribute("href") == "/tours");
    }

    [Fact]
    public void Renders_Page_Title()
    {
        // Arrange
        var tour = BuildTourDto();

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Equal("Tour Details", pageTitle.TextContent);
    }

    [Fact]
    public void Renders_Tour_Name_In_Card_Header()
    {
        // Arrange
        var tour = BuildTourDto(name: "Amazing Bike Tour 2024");

        SetupSuccessfulTourLoad(tour);

        // Act
        var cut = Render<Details>(parameters => parameters
            .Add(p => p.Id, tour.Id));

        cut.WaitForAssertion(() => cut.Find(".card-header h3"));

        // Assert
        var cardHeader = cut.Find(".card-header h3");
        Assert.Equal("Amazing Bike Tour 2024", cardHeader.TextContent);
    }

    private void SetupSuccessfulTourLoad(GetTourDto tour)
    {
        _fakeToursApi.AddTour(tour);
    }
}
