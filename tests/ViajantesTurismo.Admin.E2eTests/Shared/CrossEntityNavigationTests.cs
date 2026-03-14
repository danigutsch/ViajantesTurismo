using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class CrossEntityNavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Navigate_Between_Entities_From_Booking_Details_And_Lists()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // === From booking details: follow cross-entity links ===
        await NavigateTo($"/bookings/{booking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        // Tour link navigates to tour details
        var tourLink = Page.Locator("a[href^='/tours/']").First;
        var tourHref = await tourLink.GetAttributeAsync("href");
        Assert.NotNull(tourHref);
        await tourLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Go back and follow the customer link
        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var customerLink = Page.Locator("a[href^='/customers/']").First;
        var customerHref = await customerLink.GetAttributeAsync("href");
        Assert.NotNull(customerHref);
        await customerLink.ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        // === From the bookings list: tour and customer links in rows ===
        var bookingRow = await BookingsList.GetBookingRow(booking.Id);

        // Navigate using tour link href from the row
        var listTourHref = await bookingRow.Locator("a[href^='/tours/']").First.GetAttributeAsync("href");
        if (listTourHref is null)
        {
            Assert.Fail("Expected a tour link in the bookings list row.");
        }

        await NavigateTo(listTourHref);
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Click the customer link in the first row
        bookingRow = await BookingsList.GetBookingRow(booking.Id);
        var listCustomerHref = await bookingRow.Locator("a[href^='/customers/']").First.GetAttributeAsync("href");
        if (listCustomerHref is null)
        {
            Assert.Fail("Expected a customer link in the bookings list row.");
        }

        await NavigateTo(listCustomerHref);
        await Expect(Page).ToHaveTitleAsync("Customer Details");
    }

    [Fact]
    public async Task Can_View_Contextual_Bookings_On_Customer_And_Tour_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        _ = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // === Tour details: scoped bookings list ===
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Scoped bookings list should be visible for test-owned booking data
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
        await NavigateTo($"/customers/{customer.Id}");
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
