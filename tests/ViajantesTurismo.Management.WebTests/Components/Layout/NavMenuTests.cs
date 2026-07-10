using ViajantesTurismo.Management.Web.Components.Layout;

namespace ViajantesTurismo.Management.WebTests.Components.Layout;

public class NavMenuTests : BunitContext
{
    [Fact]
    public void Renders_brand_name()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brand = cut.Find(".navbar-brand");
        brand.TextContent.ShouldBe("ViajantesTurismo");
    }

    [Fact]
    public void Renders_home_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var homeLinks = cut.FindAll("a.nav-link[href='']");
        var homeLink = homeLinks[0];
        homeLink.TextContent.ShouldContain("Home", StringComparison.Ordinal);
        homeLink.InnerHtml.ShouldContain("bi-house-door-fill", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_tour_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        addTourLink.TextContent.ShouldContain("Add Tour", StringComparison.Ordinal);
        addTourLink.InnerHtml.ShouldContain("bi-plus-square-fill", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_tours_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toursLink = cut.Find("a[href='tours']");
        toursLink.TextContent.ShouldContain("Tours", StringComparison.Ordinal);
        toursLink.InnerHtml.ShouldContain("bi-list-nested", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_catalog_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var catalogLink = cut.Find("a[href='catalog/tours']");
        catalogLink.TextContent.ShouldContain("Catalog", StringComparison.Ordinal);
        catalogLink.InnerHtml.ShouldContain("bi-card-list", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_public_content_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var publicContentLink = cut.Find("a[href='catalog/content']");
        publicContentLink.TextContent.ShouldContain("Public Content", StringComparison.Ordinal);
        publicContentLink.InnerHtml.ShouldContain("bi-translate", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_branding_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brandingLink = cut.Find("a[href='branding']");
        brandingLink.TextContent.ShouldContain("Branding", StringComparison.Ordinal);
        brandingLink.InnerHtml.ShouldContain("bi-palette", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_bookings_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var bookingsLink = cut.Find("a[href='bookings']");
        bookingsLink.TextContent.ShouldContain("Bookings", StringComparison.Ordinal);
        bookingsLink.InnerHtml.ShouldContain("bi-calendar-check", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_customer_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        addCustomerLink.TextContent.ShouldContain("Add Customer", StringComparison.Ordinal);
        addCustomerLink.InnerHtml.ShouldContain("bi-person-plus-fill", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customers_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var customersLink = cut.Find("a[href='customers']");
        customersLink.TextContent.ShouldContain("Customers", StringComparison.Ordinal);
        customersLink.InnerHtml.ShouldContain("bi-people-fill", StringComparison.Ordinal);
    }

    [Fact]
    public void All_NavLinks_have_nav_link_class()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");
        navLinks.Count.ShouldBe(9);
    }

    [Fact]
    public void All_NavLinks_have_icons()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");
        foreach (var link in navLinks)
        {
            var icons = link.QuerySelectorAll("span[aria-hidden='true']");
            icons.ShouldNotBeEmpty();
        }
    }

    [Fact]
    public void Has_navbar_toggler_checkbox()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toggler = cut.Find("input[type='checkbox'].navbar-toggler");
        toggler.ShouldNotBeNull();
        toggler.GetAttribute("title").ShouldBe("Navigation menu");
    }

    [Fact]
    public void Has_scrollable_navigation_container()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var scrollable = cut.Find(".nav-scrollable");
        scrollable.ShouldNotBeNull();
    }

    [Fact]
    public void Navigation_uses_flex_column_layout()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav.nav.flex-column");
        nav.ShouldNotBeNull();
    }

    [Fact]
    public void All_nav_items_have_padding()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navItems = cut.FindAll(".nav-item.px-3");
        navItems.Count.ShouldBe(9);
    }

    [Fact]
    public void Top_row_has_dark_navbar_theme()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var topRow = cut.Find(".top-row.navbar.navbar-dark");
        topRow.ShouldNotBeNull();
    }

    [Fact]
    public void Brand_link_points_to_root()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brand = cut.Find(".navbar-brand");
        brand.GetAttribute("href").ShouldBe(string.Empty);
    }
}
