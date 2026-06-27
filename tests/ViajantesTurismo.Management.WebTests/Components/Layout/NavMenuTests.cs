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
        Assert.Equal("ViajantesTurismo", brand.TextContent);
    }

    [Fact]
    public void Renders_home_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var homeLinks = cut.FindAll("a.nav-link[href='']");
        var homeLink = homeLinks[0];
        Assert.Contains("Home", homeLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-house-door-fill", homeLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_tour_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        Assert.Contains("Add Tour", addTourLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-plus-square-fill", addTourLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_tours_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toursLink = cut.Find("a[href='tours']");
        Assert.Contains("Tours", toursLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-list-nested", toursLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_catalog_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var catalogLink = cut.Find("a[href='catalog/tours']");
        Assert.Contains("Catalog", catalogLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-card-list", catalogLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_public_content_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var publicContentLink = cut.Find("a[href='catalog/content']");
        Assert.Contains("Public Content", publicContentLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-translate", publicContentLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_bookings_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var bookingsLink = cut.Find("a[href='bookings']");
        Assert.Contains("Bookings", bookingsLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-calendar-check", bookingsLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_add_customer_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        Assert.Contains("Add Customer", addCustomerLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-person-plus-fill", addCustomerLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_customers_navLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var customersLink = cut.Find("a[href='customers']");
        Assert.Contains("Customers", customersLink.TextContent, StringComparison.Ordinal);
        Assert.Contains("bi-people-fill", customersLink.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void All_navLinks_have_nav_link_class()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");
        Assert.Equal(8, navLinks.Count);
    }

    [Fact]
    public void All_navLinks_have_icons()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");
        foreach (var link in navLinks)
        {
            var icons = link.QuerySelectorAll("span[aria-hidden='true']");
            Assert.NotEmpty(icons);
        }
    }

    [Fact]
    public void Has_navbar_toggler_checkbox()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toggler = cut.Find("input[type='checkbox'].navbar-toggler");
        Assert.NotNull(toggler);
        Assert.Equal("Navigation menu", toggler.GetAttribute("title"));
    }

    [Fact]
    public void Has_scrollable_navigation_container()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var scrollable = cut.Find(".nav-scrollable");
        Assert.NotNull(scrollable);
    }

    [Fact]
    public void Navigation_uses_flex_column_layout()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav.nav.flex-column");
        Assert.NotNull(nav);
    }

    [Fact]
    public void All_nav_items_have_padding()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navItems = cut.FindAll(".nav-item.px-3");
        Assert.Equal(8, navItems.Count);
    }

    [Fact]
    public void Top_row_has_dark_navbar_theme()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var topRow = cut.Find(".top-row.navbar.navbar-dark");
        Assert.NotNull(topRow);
    }

    [Fact]
    public void Brand_link_points_to_root()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brand = cut.Find(".navbar-brand");
        Assert.Equal("", brand.GetAttribute("href"));
    }
}
