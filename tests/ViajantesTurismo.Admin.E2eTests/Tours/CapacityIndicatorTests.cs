using System.Globalization;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;

namespace ViajantesTurismo.Admin.E2ETests.Tours;

public class CapacityIndicatorTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Tour_Capacity_Badges_Show_Correct_State_On_List_And_Details()
    {
        var toursListPage = new ToursListPage(Page, NavigateToAsync, ApiClient.GetAllTours);

        // === Setup: Create own tour with 3 confirmed bookings ===
        var api = ApiClient;
        var tour = await api.CreateTourAsync(minCustomers: 1, maxCustomers: 10);
        var tourName = tour.Name;

        // Create 3 customers and 3 confirmed bookings → CurrentCustomerCount = 3
        for (var i = 0; i < 3; i++)
        {
            var customer = await api.CreateCustomerAsync();
            var booking = await api.CreateBookingAsync(tour.Id, customer.Id);
            await api.ConfirmBookingAsync(booking.Id);
        }

        const int currentCount = 3;

        // === Step 1: Navigate to the tours list and verify capacity badge ===
        await NavigateToAsync("/tours");
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();

        var tourRow = await toursListPage.GetTourRow(tour.Id);
        var capacityText = await tourRow.Locator("span.text-nowrap").TextContentAsync();
        Assert.NotNull(capacityText);
        Assert.Matches(@"\d+ / \d+", capacityText);

        // === Step 2: Navigate to details and verify badge consistency ===
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();

        var capacitySection = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText($"{currentCount} / 10 customers")).ToBeVisibleAsync();

        // === Step 3: Edit tour to create "Full" state ===
        // Set MaxCustomers = 3 (= currentCount) to make it fully booked
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync("1");
        await Page.Locator("#maxCustomers").FillAsync(currentCount.ToString(CultureInfo.InvariantCulture));
        await Page.GetButton("Update Tour").ClickAsync();

        // Cancel the redirect and verify on the list
        await Page.GetButton("Cancel").ClickAsync();
        var cityRowFull = await toursListPage.GetTourRow(tour.Id);
        await Expect(cityRowFull.Locator("span.badge.bg-danger")).ToContainTextAsync("Full");
        await Expect(cityRowFull.Locator("span.text-nowrap")).ToHaveTextAsync($"{currentCount} / {currentCount}");

        // Verify details page shows "Fully Booked"
        await cityRowFull.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        var capacityFull = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityFull.Locator("span.badge.bg-danger")).ToContainTextAsync("Fully Booked");

        // === Step 4: Edit tour to create "Available spots" (green) state ===
        // Set MinCustomers = currentCount, MaxCustomers = currentCount + 3
        var greenMax = currentCount + 3;
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync(currentCount.ToString(CultureInfo.InvariantCulture));
        await Page.Locator("#maxCustomers").FillAsync(greenMax.ToString(CultureInfo.InvariantCulture));
        await Page.GetButton("Update Tour").ClickAsync();

        await Page.GetButton("Cancel").ClickAsync();

        // Tour should now show a green badge with "3 spots"
        var cityRowGreen = await toursListPage.GetTourRow(tour.Id);
        await Expect(cityRowGreen.Locator("span.badge.bg-success")).ToContainTextAsync("3 spots");

        // Verify details page shows "3 spots available"
        await cityRowGreen.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        var capacityGreen = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityGreen.Locator("span.badge.bg-success")).ToContainTextAsync("3 spots available");

        // === Step 5: Edit tour to create "Below Min" (yellow) state ===
        // Set MinCustomers higher than currentCount
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync((currentCount + 5).ToString(CultureInfo.InvariantCulture));
        await Page.Locator("#maxCustomers").FillAsync("20");
        await Page.GetButton("Update Tour").ClickAsync();

        await Page.GetButton("Cancel").ClickAsync();

        // Tour should now show a yellow "Below Min" badge
        var cityRowYellow = await toursListPage.GetTourRow(tour.Id);
        await Expect(cityRowYellow.Locator("span.badge.bg-warning")).ToContainTextAsync("Below Min");

        // Verify details page shows "Below Minimum"
        await cityRowYellow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading(tourName)).ToBeVisibleAsync();
        var capacityYellow = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityYellow.Locator("span.badge.bg-warning")).ToContainTextAsync("Below Minimum");
    }
}
