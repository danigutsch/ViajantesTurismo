using ViajantesTurismo.Admin.Web.Components.Layout;

namespace ViajantesTurismo.Admin.WebTests;

public class NavMenuTests : BunitContext
{
    [Fact]
    public void Renders_Brand_Name()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brand = cut.Find(".navbar-brand");
        Assert.Equal("ViajantesTurismo", brand.TextContent);
    }

    [Fact]
    public void Renders_Home_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var homeLinks = cut.FindAll("a.nav-link[href='']");
        var homeLink = homeLinks[0];
        Assert.Contains("Home", homeLink.TextContent);
        Assert.Contains("bi-house-door-fill", homeLink.InnerHtml);
    }

    [Fact]
    public void Renders_Add_Tour_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addTourLink = cut.Find("a[href='addtour']");
        Assert.Contains("Add Tour", addTourLink.TextContent);
        Assert.Contains("bi-plus-square-fill", addTourLink.InnerHtml);
    }

    [Fact]
    public void Renders_Tours_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toursLink = cut.Find("a[href='tours']");
        Assert.Contains("Tours", toursLink.TextContent);
        Assert.Contains("bi-list-nested", toursLink.InnerHtml);
    }

    [Fact]
    public void Renders_Bookings_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var bookingsLink = cut.Find("a[href='bookings']");
        Assert.Contains("Bookings", bookingsLink.TextContent);
        Assert.Contains("bi-calendar-check", bookingsLink.InnerHtml);
    }

    [Fact]
    public void Renders_Add_Customer_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var addCustomerLink = cut.Find("a[href='customers/create']");
        Assert.Contains("Add Customer", addCustomerLink.TextContent);
        Assert.Contains("bi-person-plus-fill", addCustomerLink.InnerHtml);
    }

    [Fact]
    public void Renders_Customers_NavLink()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var customersLink = cut.Find("a[href='customers']");
        Assert.Contains("Customers", customersLink.TextContent);
        Assert.Contains("bi-people-fill", customersLink.InnerHtml);
    }

    [Fact]
    public void All_NavLinks_Have_Nav_Link_Class()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navLinks = cut.FindAll(".nav-link");
        Assert.Equal(6, navLinks.Count);
    }

    [Fact]
    public void All_NavLinks_Have_Icons()
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
    public void Has_Navbar_Toggler_Checkbox()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var toggler = cut.Find("input[type='checkbox'].navbar-toggler");
        Assert.NotNull(toggler);
        Assert.Equal("Navigation menu", toggler.GetAttribute("title"));
    }

    [Fact]
    public void Has_Scrollable_Navigation_Container()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var scrollable = cut.Find(".nav-scrollable");
        Assert.NotNull(scrollable);
    }

    [Fact]
    public void Navigation_Uses_Flex_Column_Layout()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var nav = cut.Find("nav.nav.flex-column");
        Assert.NotNull(nav);
    }

    [Fact]
    public void All_Nav_Items_Have_Padding()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var navItems = cut.FindAll(".nav-item.px-3");
        Assert.Equal(6, navItems.Count);
    }

    [Fact]
    public void Top_Row_Has_Dark_Navbar_Theme()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var topRow = cut.Find(".top-row.navbar.navbar-dark");
        Assert.NotNull(topRow);
    }

    [Fact]
    public void Brand_Link_Points_To_Root()
    {
        // Act
        var cut = Render<NavMenu>();

        // Assert
        var brand = cut.Find(".navbar-brand");
        Assert.Equal("", brand.GetAttribute("href"));
    }
}
