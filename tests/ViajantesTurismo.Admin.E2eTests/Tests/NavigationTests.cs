using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public partial class NavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task DeepLinkingAllRoutes()
    {
        // === Static routes ===
        await NavigateToAsync("/");
        await Expect(Page).ToHaveTitleAsync("Home - ViajantesTurismo");

        await NavigateToAsync("/addtour");
        await Expect(Page).ToHaveTitleAsync("Add Tour");

        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        await NavigateToAsync("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // === Parameterized routes: grab IDs from list pages ===

        // Tour routes
        await NavigateToAsync("/tours");
        var tourViewHref = await Page.Locator("table tbody tr").First
            .GetLink("View").GetAttributeAsync("href");
        var tourId = tourViewHref!.Split('/').Last();

        await NavigateToAsync($"/tours/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await NavigateToAsync($"/edittour/{tourId}");
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        // Customer routes
        await NavigateToAsync("/customers");
        var customerViewHref = await Page.Locator("table tbody tr").First
            .GetLink("View").GetAttributeAsync("href");
        var customerId = customerViewHref!.Split('/').Last();

        await NavigateToAsync($"/customers/{customerId}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        await NavigateToAsync($"/customers/{customerId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Customer");

        // Booking routes
        await NavigateToAsync("/bookings");
        var bookingViewHref = await Page.Locator("table tbody tr").First
            .GetLink("View").GetAttributeAsync("href");
        var bookingId = bookingViewHref!.Split('/').Last();

        await NavigateToAsync($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        await NavigateToAsync($"/bookings/{bookingId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // === Customer wizard redirect and steps ===
        await NavigateToAsync("/customers/create");
        await Expect(Page).ToHaveURLAsync(PersonalInfoRegex());
        await Expect(Page).ToHaveTitleAsync("Create Customer - Personal Information");

        await NavigateToAsync("/customers/create/identification");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Identification");

        await NavigateToAsync("/customers/create/contact");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Contact Information");

        await NavigateToAsync("/customers/create/address");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Address");

        await NavigateToAsync("/customers/create/physical");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Physical Information");

        await NavigateToAsync("/customers/create/accommodation");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Accommodation Preferences");

        await NavigateToAsync("/customers/create/emergency-contact");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Emergency Contact");

        await NavigateToAsync("/customers/create/medical");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Medical Information");

        await NavigateToAsync("/customers/create/review");
        await Expect(Page).ToHaveTitleAsync("Create Customer - Review & Submit");

        // === Browser back from detail → list preserves data ===
        await NavigateToAsync("/tours");
        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();
        await Page.Locator("table tbody tr").First.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GoBackAsync(new PageGoBackOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await Expect(Page).ToHaveTitleAsync("Tours");
        await Expect(Page.Locator("table tbody tr").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HomePageAndNavigation()
    {
        // === Home Page Content ===
        await NavigateToAsync("/");

        await Expect(Page.GetHeading("ViajantesTurismo Admin Dashboard")).ToBeVisibleAsync();

        // 3 dashboard cards
        await Expect(Page.GetHeading("Tours Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Customer Management")).ToBeVisibleAsync();
        await Expect(Page.GetHeading("Bookings Management")).ToBeVisibleAsync();

        // Quick action buttons on the dashboard cards
        var content = Page.Locator("article.content");
        await Expect(content.GetLink("Add Tour")).ToBeVisibleAsync();
        await Expect(content.GetLink("Add Customer")).ToBeVisibleAsync();
        await Expect(content.GetLink("View All")).ToHaveCountAsync(3);

        // About link in top row
        await Expect(Page.GetLink("About")).ToBeVisibleAsync();

        // === Sidebar Navigation: each link reaches correct URL and is highlighted ===
        var sidebar = Page.Locator(".sidebar");

        // Home is active on the home page
        await Expect(sidebar.GetLink("Home")).ToHaveClassAsync(ActiveRegex());

        // Tours
        await sidebar.GetLink("Tours", exact: true).ClickAsync();
        await Expect(Page).ToHaveURLAsync(ToursRegex());
        await Expect(sidebar.GetLink("Tours", exact: true)).ToHaveClassAsync(ActiveRegex());

        // Bookings
        await sidebar.GetLink("Bookings").ClickAsync();
        await Expect(Page).ToHaveURLAsync(BookingsRegex());
        await Expect(sidebar.GetLink("Bookings")).ToHaveClassAsync(ActiveRegex());

        // Customers
        await sidebar.GetLink("Customers", exact: true).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CustomersRegex());
        await Expect(sidebar.GetLink("Customers", exact: true)).ToHaveClassAsync(ActiveRegex());

        // Add Customer
        await sidebar.GetLink("Add Customer").ClickAsync();
        await Expect(Page).ToHaveURLAsync(CreateCustomerRegex());
        await Expect(sidebar.GetLink("Add Customer")).ToHaveClassAsync(ActiveRegex());

        // Add Tour
        await sidebar.GetLink("Add Tour").ClickAsync();
        await Expect(Page).ToHaveURLAsync(AddTourRegex());
        await Expect(sidebar.GetLink("Add Tour")).ToHaveClassAsync(ActiveRegex());

        // Home (back)
        await sidebar.GetLink("Home").ClickAsync();
        await Expect(sidebar.GetLink("Home")).ToHaveClassAsync(ActiveRegex());

        // === Quick Action Button Navigation ===
        // "Add Tour" card → /addtour
        await content.GetLink("Add Tour").ClickAsync();
        await Expect(Page).ToHaveURLAsync(AddTourRegex());

        await NavigateToAsync("/");

        // "Add Customer" card → /customers/create
        await content.GetLink("Add Customer").ClickAsync();
        await Expect(Page).ToHaveURLAsync(CreateCustomerRegex());

        await NavigateToAsync("/");

        // "View All" buttons: Tours, Customers, Bookings
        var viewAllLinks = content.GetLink("View All");

        await viewAllLinks.Nth(0).ClickAsync();
        await Expect(Page).ToHaveURLAsync(ToursRegex());

        await NavigateToAsync("/");
        await viewAllLinks.Nth(1).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CustomersRegex());

        await NavigateToAsync("/");
        await viewAllLinks.Nth(2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(BookingsRegex());

        // === Prefix Match: /tours/{id} still highlights "Tours" nav link ===
        await NavigateToAsync("/tours");
        await content.GetLink("View").First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(TourRegex());
        await Expect(sidebar.GetLink("Tours", exact: true)).ToHaveClassAsync(ActiveRegex());
    }

    [GeneratedRegex("active")]
    private static partial Regex ActiveRegex();

    [GeneratedRegex("/tours$")]
    private static partial Regex ToursRegex();

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
