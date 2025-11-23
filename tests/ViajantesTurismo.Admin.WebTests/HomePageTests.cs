using ViajantesTurismo.Admin.Web.Components.Pages;

namespace ViajantesTurismo.Admin.WebTests;

public class HomePageTests : BunitContext
{
    [Fact]
    public void Renders_Page_Title()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Contains("ViajantesTurismo Admin Dashboard", pageTitle.TextContent);
        Assert.Contains("bi-bicycle", pageTitle.InnerHtml);
    }

    [Fact]
    public void Renders_Welcome_Message()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var lead = cut.Find("p.lead");
        Assert.Contains("Welcome to the ViajantesTurismo administration system", lead.TextContent);
    }

    [Fact]
    public void Renders_Tours_Management_Card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var toursCard = cards.First(c => c.TextContent.Contains("Tours Management"));

        Assert.Contains("Create and manage bike tour packages", toursCard.TextContent);
        Assert.Contains("bi-bicycle", toursCard.InnerHtml);
    }

    [Fact]
    public void Renders_Customer_Management_Card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var customersCard = cards.First(c => c.TextContent.Contains("Customer Management"));

        Assert.Contains("Manage customer profiles", customersCard.TextContent);
        Assert.Contains("bi-people", customersCard.InnerHtml);
    }

    [Fact]
    public void Renders_Bookings_Management_Card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var bookingsCard = cards.First(c => c.TextContent.Contains("Bookings Management"));

        Assert.Contains("Track and manage customer bookings", bookingsCard.TextContent);
        Assert.Contains("bi-calendar-check", bookingsCard.InnerHtml);
    }

    [Fact]
    public void Renders_Add_Tour_Link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        Assert.Contains("Add Tour", addTourLink.TextContent);
        Assert.Contains("btn-primary", addTourLink.ClassName);
    }

    [Fact]
    public void Renders_View_All_Tours_Link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewToursLink = cut.Find("a[href='tours']");
        Assert.Contains("View All", viewToursLink.TextContent);
        Assert.Contains("btn-outline-primary", viewToursLink.ClassName);
    }

    [Fact]
    public void Renders_Add_Customer_Link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        Assert.Contains("Add Customer", addCustomerLink.TextContent);
        Assert.Contains("btn-success", addCustomerLink.ClassName);
    }

    [Fact]
    public void Renders_View_All_Customers_Link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewCustomersLink = cut.Find("a[href='customers']");
        Assert.Contains("View All", viewCustomersLink.TextContent);
        Assert.Contains("btn-outline-success", viewCustomersLink.ClassName);
    }

    [Fact]
    public void Renders_View_All_Bookings_Link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewBookingsLink = cut.Find("a[href='bookings']");
        Assert.Contains("View All", viewBookingsLink.TextContent);
        Assert.Contains("btn-outline-info", viewBookingsLink.ClassName);
    }

    [Fact]
    public void Renders_About_Section()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var aboutCard = cards.First(c => c.TextContent.Contains("About ViajantesTurismo"));

        Assert.Contains("This administrative platform helps you efficiently manage", aboutCard.TextContent);
    }

    [Fact]
    public void All_Cards_Have_Bootstrap_Border_Classes()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card.border-primary, .card.border-success, .card.border-info");
        Assert.Equal(3, cards.Count);
    }

    [Fact]
    public void Uses_Bootstrap_Grid_Layout()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var containerFluid = cut.Find(".container-fluid");
        Assert.NotNull(containerFluid);

        var rows = cut.FindAll(".row");
        Assert.True(rows.Count >= 3); // At least 3 rows
    }
}
