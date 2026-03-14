using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public partial class NavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Deep_Link_All_Routes()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await AssertDeepLink("/", "Home - ViajantesTurismo");
        await AssertDeepLink("/addtour", "Add Tour");
        await AssertDeepLink("/tours", "Tours");
        await AssertDeepLink("/customers", "Customers");
        await AssertDeepLink("/bookings", "Bookings");

        await AssertDeepLink($"/tours/{tour.Id}", "Tour Details");
        await AssertDeepLink($"/edittour/{tour.Id}", "Edit Tour");
        await AssertDeepLink($"/customers/{customer.Id}", "Customer Details");
        await AssertDeepLink($"/customers/{customer.Id}/edit", "Edit Customer");
        await AssertDeepLink($"/bookings/{booking.Id}", "Booking Details");
        await AssertDeepLink($"/bookings/{booking.Id}/edit", "Edit Booking");

        await AssertCustomerWizardDeepLink("/customers/create", PersonalInfoRegex(), "Create Customer - Personal Information");
        await AssertDeepLink("/customers/create/identification", "Create Customer - Identification");
        await AssertDeepLink("/customers/create/contact", "Create Customer - Contact Information");
        await AssertDeepLink("/customers/create/address", "Create Customer - Address");
        await AssertDeepLink("/customers/create/physical", "Create Customer - Physical Information");
        await AssertDeepLink("/customers/create/accommodation", "Create Customer - Accommodation Preferences");
        await AssertDeepLink("/customers/create/emergency-contact", "Create Customer - Emergency Contact");
        await AssertDeepLink("/customers/create/medical", "Create Customer - Medical Information");
        await AssertDeepLink("/customers/create/review", "Create Customer - Review & Submit");

        // Assert
        await NavigateTo("/tours");
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page).ToHaveTitleAsync("Tours");
        await Expect(Page).ToHaveURLAsync(ToursRegex());
    }

    [Fact]
    public async Task Can_Render_Home_Page_And_Navigate_Using_Sidebar_And_Quick_Actions()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var sidebar = Page.Locator(".sidebar");

        // Act
        await NavigateTo("/");

        // Assert
        await Expect(Page.GetHeading("ViajantesTurismo Admin Dashboard")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Tours Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Customer Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Bookings Management")).ToBeVisibleAsync();

        var content = Page.Locator("article.content");
        await Expect(content.GetLink("Add Tour")).ToBeVisibleAsync();
        await Expect(content.GetLink("Add Customer")).ToBeVisibleAsync();
        await Expect(content.GetLink("View All")).ToHaveCountAsync(3);
        await Expect(Page.GetLink("About")).ToBeVisibleAsync();
        await Expect(sidebar.GetLink("Home")).ToHaveClassAsync(ActiveRegex());

        // Act/Assert
        await AssertSidebarNavigation(sidebar, "Tours", ToursRegex(), exact: true);
        await AssertSidebarNavigation(sidebar, "Bookings", BookingsRegex());
        await AssertSidebarNavigation(sidebar, "Customers", CustomersRegex(), exact: true);
        await AssertSidebarNavigation(sidebar, "Add Customer", CreateCustomerRegex());
        await AssertSidebarNavigation(sidebar, "Add Tour", AddTourRegex());
        await AssertSidebarNavigation(sidebar, "Home", HomeRegex());

        await content.GetLink("Add Tour").ClickAsync();
        await Expect(Page).ToHaveURLAsync(AddTourRegex());

        await NavigateTo("/");

        await content.GetLink("Add Customer").ClickAsync();
        await Expect(Page).ToHaveURLAsync(CreateCustomerRegex());

        await NavigateTo("/");

        var viewAllLinks = content.GetLink("View All");

        await viewAllLinks.Nth(0).ClickAsync();
        await Expect(Page).ToHaveURLAsync(ToursRegex());

        await NavigateTo("/");
        await viewAllLinks.Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CustomersRegex());

        await NavigateTo("/");
        await viewAllLinks.Nth(2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(BookingsRegex());

        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveURLAsync(TourRegex());
        await Expect(sidebar.GetLink("Tours", exact: true)).ToHaveClassAsync(ActiveRegex());
    }

    private async Task AssertDeepLink(string path, string expectedTitle)
    {
        await NavigateTo(path);
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }

    private async Task AssertCustomerWizardDeepLink(string path, Regex expectedUrl, string expectedTitle)
    {
        await NavigateTo(path);
        await Expect(Page).ToHaveURLAsync(expectedUrl);
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }

    private async Task AssertSidebarNavigation(ILocator sidebar, string linkName, Regex expectedUrl, bool? exact = null)
    {
        await sidebar.GetLink(linkName, exact).ClickAsync();
        await Expect(Page).ToHaveURLAsync(expectedUrl);
        await Expect(sidebar.GetLink(linkName, exact)).ToHaveClassAsync(ActiveRegex());
    }

    [GeneratedRegex("active")]
    private static partial Regex ActiveRegex();

    [GeneratedRegex("/tours$")]
    private static partial Regex ToursRegex();

    [GeneratedRegex("/$")]
    private static partial Regex HomeRegex();

    [GeneratedRegex("/addtour$")]
    private static partial Regex AddTourRegex();

    [GeneratedRegex(@"/tours/[\da-f-]+")]
    private static partial Regex TourRegex();

    [GeneratedRegex("/bookings$")]
    private static partial Regex BookingsRegex();

    [GeneratedRegex("/customers$")]
    private static partial Regex CustomersRegex();

    [GeneratedRegex("/customers/create")]
    private static partial Regex CreateCustomerRegex();

    [GeneratedRegex("/customers/create/personal-info$")]
    private static partial Regex PersonalInfoRegex();
}
