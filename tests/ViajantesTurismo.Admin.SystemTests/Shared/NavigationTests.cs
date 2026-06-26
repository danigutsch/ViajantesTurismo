using TestTraits = ViajantesTurismo.Admin.SystemTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

[Trait(TestTraits.CategoryName, TestTraits.MigrationCategory)]
[Trait(TestTraits.ScopeName, TestTraits.SystemScope)]
[Trait(TestTraits.AreaName, TestTraits.SharedArea)]
[Trait(TestTraits.HostName, TestTraits.AspireHost)]
public class NavigationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private const string AddTourTitle = "Add Tour";
    private const string ToursTitle = "Tours";

    [Fact]
    public async Task Can_Deep_Link_All_Routes()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/", "Home - ViajantesTurismo");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/addtour", AddTourTitle);
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/tours", ToursTitle);
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers", "Customers");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/bookings", "Bookings");

        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/tours/{tour.Id}", "Tour Details");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/edittour/{tour.Id}", "Edit Tour");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/customers/{customer.Id}", "Customer Details");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/customers/{customer.Id}/edit", "Edit Customer");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/bookings/{booking.Id}", "Booking Details");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, $"/bookings/{booking.Id}/edit", "Edit Booking");

        await NavigationTestHelpers.AssertCustomerWizardDeepLink(Page, NavigateTo, NavigationTestRegexes.PersonalInfo(), "Create Customer - Personal Information");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/identification", "Create Customer - Identification");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/contact", "Create Customer - Contact Information");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/address", "Create Customer - Address");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/physical", "Create Customer - Physical Information");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/accommodation", "Create Customer - Accommodation Preferences");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/emergency-contact", "Create Customer - Emergency Contact");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/medical", "Create Customer - Medical Information");
        await NavigationTestHelpers.AssertDeepLink(Page, NavigateTo, "/customers/create/review", "Create Customer - Review & Submit");
        await NavigateTo("/tours");
        await NavigateTo($"/tours/{tour.Id}");
        await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Assert
        // Deep-link assertions are verified inside the navigation helpers above; this final block checks browser back-navigation.
        await Expect(Page).ToHaveTitleAsync(ToursTitle);
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.Tours());
    }

    [Fact]
    public async Task Can_Render_Home_Page_With_Dashboard_Content_And_Primary_Links()
    {
        // Arrange
        var sidebar = Page.Locator(".sidebar");

        // Act
        await NavigateTo("/");

        // Assert
        await Expect(Page.GetHeading("ViajantesTurismo Admin Dashboard")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Tours Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Customer Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Bookings Management")).ToBeVisibleAsync();

        var content = Page.Locator("article.content");
        await Expect(content.GetLink(AddTourTitle)).ToBeVisibleAsync();
        await Expect(content.GetLink("Add Customer")).ToBeVisibleAsync();
        await Expect(content.GetLink("View All")).ToHaveCountAsync(3);
        await Expect(Page.GetLink("About")).ToBeVisibleAsync();
        await Expect(sidebar.GetLink("Home")).ToHaveClassAsync(NavigationTestRegexes.Active());
    }

    [Fact]
    public async Task Sidebar_Navigation_Should_Update_Active_State_For_Primary_Routes()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var sidebar = Page.Locator(".sidebar");

        // Act
        await NavigateTo("/");

        // Assert
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, "Tours", NavigationTestRegexes.Tours(), exact: true);
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, "Bookings", NavigationTestRegexes.Bookings());
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, "Customers", NavigationTestRegexes.Customers(), exact: true);
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, "Add Customer", NavigationTestRegexes.CreateCustomer());
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, AddTourTitle, NavigationTestRegexes.AddTour());
        await NavigationTestHelpers.AssertSidebarNavigation(Page, sidebar, "Home", NavigationTestRegexes.Home());

        // Act
        await NavigateTo($"/tours/{tour.Id}");

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.Tour());
        await Expect(sidebar.GetLink("Tours", exact: true)).ToHaveClassAsync(NavigationTestRegexes.Active());
    }

    [Fact]
    public async Task Quick_Actions_Should_Navigate_To_Target_Pages()
    {
        // Arrange
        // No additional setup required beyond navigating to the dashboard before each quick-action check.

        // Act
        await NavigateTo("/");
        var content = Page.Locator("article.content");
        await content.GetLink(AddTourTitle).ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.AddTour());

        // Act
        await NavigateTo("/");
        await content.GetLink("Add Customer").ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.CreateCustomer());

        // Act
        await NavigateTo("/");
        var viewAllLinks = content.GetLink("View All");

        await viewAllLinks.Nth(0).ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.Tours());

        // Act
        await NavigateTo("/");
        await viewAllLinks.Nth(1).ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.Customers());

        // Act
        await NavigateTo("/");
        await viewAllLinks.Nth(2).ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(NavigationTestRegexes.Bookings());
    }
}
