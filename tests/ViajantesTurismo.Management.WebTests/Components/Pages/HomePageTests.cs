using ViajantesTurismo.Management.Web.Components.Pages;

namespace ViajantesTurismo.Management.WebTests.Components.Pages;

public class HomePageTests : BunitContext
{
    [Fact]
    public void Renders_page_title()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var pageTitle = cut.Find("h1");
        (pageTitle.TextContent).ShouldContain("ViajantesTurismo Admin Dashboard", StringComparison.Ordinal);
        (pageTitle.InnerHtml).ShouldContain("bi-bicycle", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_welcome_message()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var lead = cut.Find("p.lead");
        (lead.TextContent).ShouldContain("Welcome to the ViajantesTurismo administration system", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_tours_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var toursCard = cards.First(c => c.TextContent.Contains("Tours Management", StringComparison.Ordinal));

        (toursCard.TextContent).ShouldContain("Create and manage bike tour packages", StringComparison.Ordinal);
        (toursCard.InnerHtml).ShouldContain("bi-bicycle", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customer_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var customersCard = cards.First(c => c.TextContent.Contains("Customer Management", StringComparison.Ordinal));

        (customersCard.TextContent).ShouldContain("Manage customer profiles", StringComparison.Ordinal);
        (customersCard.InnerHtml).ShouldContain("bi-people", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_bookings_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var bookingsCard = cards.First(c => c.TextContent.Contains("Bookings Management", StringComparison.Ordinal));

        (bookingsCard.TextContent).ShouldContain("Track and manage customer bookings", StringComparison.Ordinal);
        (bookingsCard.InnerHtml).ShouldContain("bi-calendar-check", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_tour_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        (addTourLink.TextContent).ShouldContain("Add Tour", StringComparison.Ordinal);
        (addTourLink.ClassName).ShouldContain("btn-primary", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_tours_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewToursLink = cut.Find("a[href='tours']");
        (viewToursLink.TextContent).ShouldContain("View All", StringComparison.Ordinal);
        (viewToursLink.ClassName).ShouldContain("btn-outline-primary", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_customer_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        (addCustomerLink.TextContent).ShouldContain("Add Customer", StringComparison.Ordinal);
        (addCustomerLink.ClassName).ShouldContain("btn-success", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_customers_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewCustomersLink = cut.Find("a[href='customers']");
        (viewCustomersLink.TextContent).ShouldContain("View All", StringComparison.Ordinal);
        (viewCustomersLink.ClassName).ShouldContain("btn-outline-success", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_bookings_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewBookingsLink = cut.Find("a[href='bookings']");
        (viewBookingsLink.TextContent).ShouldContain("View All", StringComparison.Ordinal);
        (viewBookingsLink.ClassName).ShouldContain("btn-outline-info", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_about_section()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var aboutCard = cards.First(c => c.TextContent.Contains("About ViajantesTurismo", StringComparison.Ordinal));

        (aboutCard.TextContent).ShouldContain("This administrative platform helps you efficiently manage", StringComparison.Ordinal);
    }

    [Fact]
    public void All_cards_have_bootstrap_border_classes()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card.border-primary, .card.border-success, .card.border-info");
        (cards.Count).ShouldBe(3);
    }

    [Fact]
    public void Uses_bootstrap_grid_layout()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var containerFluid = cut.Find(".container-fluid");
        _ = (containerFluid).ShouldNotBeNull();

        var rows = cut.FindAll(".row");
        (rows.Count >= 3).ShouldBeTrue(); // At least 3 rows
    }
}
