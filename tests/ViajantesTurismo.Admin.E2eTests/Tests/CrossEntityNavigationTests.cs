using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class CrossEntityNavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Navigate_Between_Entities_From_Booking_Details_And_Lists()
    {
        // === From booking details: follow cross-entity links ===
        await NavigateToAsync("/bookings");
        var firstRow = Page.Locator("table tbody tr").First;
        await firstRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        // Tour link navigates to tour details
        var tourLink = Page.Locator("a[href^='/tours/']").First;
        var tourHref = await tourLink.GetAttributeAsync("href");
        Assert.NotNull(tourHref);
        await tourLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Go back and follow customer link
        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var customerLink = Page.Locator("a[href^='/customers/']").First;
        var customerHref = await customerLink.GetAttributeAsync("href");
        Assert.NotNull(customerHref);
        await customerLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        // === From bookings list: tour and customer links in rows ===
        await NavigateToAsync("/bookings");
        var rows = Page.Locator("table tbody tr");
        var rowCount = await rows.CountAsync();
        Assert.True(rowCount > 0);

        // Click tour link in first row
        var listTourLink = rows.First.Locator("a[href^='/tours/']").First;
        await listTourLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Click customer link in first row
        var listCustomerLink = rows.First.Locator("a[href^='/customers/']").First;
        await listCustomerLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");
    }

    [Fact]
    public async Task Can_View_Contextual_Bookings_On_Customer_And_Tour_Details()
    {
        // === Tour details: scoped bookings list ===
        await NavigateToAsync("/tours");
        var tourRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Scoped bookings list should be visible (tour has bookings from seeded data)
        var tourBookingsTable = Page.Locator("table");
        await Expect(tourBookingsTable).ToBeVisibleAsync();

        // Tour column should be hidden (ShowTourInfo=false)
        var tourColumnHeaders = tourBookingsTable.Locator("th:has-text('Tour')");
        await Expect(tourColumnHeaders).ToHaveCountAsync(0);

        // Customer column should be visible
        var customerColumn = tourBookingsTable.Locator("th:has-text('Customer')");
        await Expect(customerColumn).ToBeVisibleAsync();

        // Booking links should navigate to booking details
        var bookingViewLink = tourBookingsTable.Locator("tbody tr").First.GetLink("View");
        await bookingViewLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        // === Customer details: scoped bookings list ===
        await NavigateToAsync("/customers");
        var customerRow = Page.Locator("table tbody tr").First;
        await customerRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        // Scoped bookings list should be visible
        var customerBookingsTable = Page.Locator("table");
        if (await customerBookingsTable.CountAsync() > 0)
        {
            // Customer column should be hidden (ShowCustomerInfo=false)
            var customerHeaders = customerBookingsTable.Locator("th:has-text('Customer')");
            await Expect(customerHeaders).ToHaveCountAsync(0);

            // Tour column should be visible
            var tourColumn = customerBookingsTable.Locator("th:has-text('Tour')");
            await Expect(tourColumn).ToBeVisibleAsync();
        }
    }
}
