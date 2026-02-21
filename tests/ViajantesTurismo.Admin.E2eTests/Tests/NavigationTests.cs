using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2eTests.Tests;

public partial class NavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task HomePageAndNavigation()
    {
        // === Home Page Content ===
        await NavigateToAsync("/");

        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "ViajantesTurismo Admin Dashboard" }))
            .ToBeVisibleAsync();

        // 3 dashboard cards
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Tours Management" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Customer Management" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Bookings Management" })).ToBeVisibleAsync();

        // Quick action buttons on the dashboard cards
        var content = Page.Locator("article.content");
        await Expect(content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Tour" })).ToBeVisibleAsync();
        await Expect(content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Customer" })).ToBeVisibleAsync();
        await Expect(content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View All" })).ToHaveCountAsync(3);

        // About link in top row
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "About" })).ToBeVisibleAsync();

        // === Sidebar Navigation: each link reaches correct URL and is highlighted ===
        var sidebar = Page.Locator(".sidebar");

        // Home is active on the home page
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Home" }))
            .ToHaveClassAsync(ActiveRegex());

        // Tours
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Tours", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(ToursRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Tours", Exact = true }))
            .ToHaveClassAsync(ActiveRegex());

        // Bookings
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Bookings" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(BookingsRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Bookings" }))
            .ToHaveClassAsync(ActiveRegex());

        // Customers
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Customers", Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CustomersRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Customers", Exact = true }))
            .ToHaveClassAsync(ActiveRegex());

        // Add Customer
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Customer" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CreateCustomerRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Customer" }))
            .ToHaveClassAsync(ActiveRegex());

        // Add Tour
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Tour" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(AddTourRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Tour" }))
            .ToHaveClassAsync(ActiveRegex());

        // Home (back)
        await sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Home" }).ClickAsync();
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Home" }))
            .ToHaveClassAsync(ActiveRegex());

        // === Quick Action Button Navigation ===
        // "Add Tour" card → /addtour
        await content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Tour" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(AddTourRegex());

        await NavigateToAsync("/");

        // "Add Customer" card → /customers/create
        await content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Add Customer" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(CreateCustomerRegex());

        await NavigateToAsync("/");

        // "View All" buttons: Tours, Customers, Bookings
        var viewAllLinks = content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View All" });

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
        await content.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "View" }).First.ClickAsync();
        await Expect(Page).ToHaveURLAsync(TourRegex());
        await Expect(sidebar.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Tours", Exact = true }))
            .ToHaveClassAsync(ActiveRegex());
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
}
