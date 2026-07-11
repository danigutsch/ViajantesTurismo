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
        TestAssert.Contains("ViajantesTurismo Admin Dashboard", pageTitle.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-bicycle", pageTitle.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_welcome_message()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var lead = cut.Find("p.lead");
        TestAssert.Contains("Welcome to the ViajantesTurismo administration system", lead.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_tours_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var toursCard = cards.First(c => c.TextContent.Contains("Tours Management", StringComparison.Ordinal));

        TestAssert.Contains("Create and manage bike tour packages", toursCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-bicycle", toursCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customer_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var customersCard = cards.First(c => c.TextContent.Contains("Customer Management", StringComparison.Ordinal));

        TestAssert.Contains("Manage customer profiles", customersCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-people", customersCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_bookings_management_card()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var bookingsCard = cards.First(c => c.TextContent.Contains("Bookings Management", StringComparison.Ordinal));

        TestAssert.Contains("Track and manage customer bookings", bookingsCard.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("bi-calendar-check", bookingsCard.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_tour_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        TestAssert.Contains("Add Tour", addTourLink.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-primary", addTourLink.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_tours_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewToursLink = cut.Find("a[href='tours']");
        TestAssert.Contains("View All", viewToursLink.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-outline-primary", viewToursLink.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_customer_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        TestAssert.Contains("Add Customer", addCustomerLink.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-success", addCustomerLink.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_customers_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewCustomersLink = cut.Find("a[href='customers']");
        TestAssert.Contains("View All", viewCustomersLink.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-outline-success", viewCustomersLink.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_view_all_bookings_link()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var viewBookingsLink = cut.Find("a[href='bookings']");
        TestAssert.Contains("View All", viewBookingsLink.TextContent, StringComparison.Ordinal);
        TestAssert.Contains("btn-outline-info", viewBookingsLink.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_about_section()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card");
        var aboutCard = cards.First(c => c.TextContent.Contains("About ViajantesTurismo", StringComparison.Ordinal));

        TestAssert.Contains("This administrative platform helps you efficiently manage", aboutCard.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void All_cards_have_bootstrap_border_classes()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var cards = cut.FindAll(".card.border-primary, .card.border-success, .card.border-info");
        TestAssert.Equal(3, cards.Count);
    }

    [Fact]
    public void Uses_bootstrap_grid_layout()
    {
        // Act
        var cut = Render<Home>();

        // Assert
        var containerFluid = cut.Find(".container-fluid");
        _ = TestAssert.NotNull(containerFluid);

        var rows = cut.FindAll(".row");
        TestAssert.True(rows.Count >= 3); // At least 3 rows
    }
}
