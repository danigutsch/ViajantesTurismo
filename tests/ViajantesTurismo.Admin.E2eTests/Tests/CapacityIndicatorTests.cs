using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class CapacityIndicatorTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_Capacity_Badges_Show_Correct_State_On_List_And_Details()
    {
        // === Step 1: Navigate to tours list and verify capacity badges exist ===
        await NavigateToAsync("/tours");
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();

        // Every tour should have exactly one capacity badge (warning, danger, or success)
        var tourRows = Page.Locator("table tbody tr");
        var rowCount = await tourRows.CountAsync();
        Assert.True(rowCount >= 5, "Expected at least 5 tours");

        // City Highlights should have a capacity badge
        var cityRow = tourRows.Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        var capacityText = await cityRow.Locator("span.text-nowrap").TextContentAsync();
        Assert.NotNull(capacityText);
        Assert.Matches(@"\d+ / \d+", capacityText);

        // Read the current count from the list to use in subsequent steps
        var parts = capacityText.Split(" / ");
        var currentCount = int.Parse(parts[0]);
        var maxCustomers = int.Parse(parts[1]);

        // === Step 2: Navigate to details and verify badge consistency ===
        await cityRow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading("City Highlights")).ToBeVisibleAsync();

        var capacitySection = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText($"{currentCount} / {maxCustomers} customers")).ToBeVisibleAsync();

        // === Step 3: Edit tour to create "Full" state ===
        // Set MaxCustomers = CurrentCount to make it fully booked
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync("1");
        await Page.Locator("#maxCustomers").FillAsync(currentCount.ToString());
        await Page.GetButton("Update Tour").ClickAsync();

        // Cancel the redirect and verify on the list
        await Page.GetButton("Cancel").ClickAsync();
        await NavigateToAsync("/tours");

        var cityRowFull = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await Expect(cityRowFull.Locator("span.badge.bg-danger")).ToContainTextAsync("Full");
        await Expect(cityRowFull.Locator("span.text-nowrap")).ToHaveTextAsync($"{currentCount} / {currentCount}");

        // Verify details page shows "Fully Booked"
        await cityRowFull.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading("City Highlights")).ToBeVisibleAsync();
        var capacityFull = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityFull.Locator("span.badge.bg-danger")).ToContainTextAsync("Fully Booked");

        // === Step 4: Edit tour to create "Available spots" (green) state ===
        // Set MinCustomers = currentCount, MaxCustomers = currentCount + 3
        var greenMax = currentCount + 3;
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync(currentCount.ToString());
        await Page.Locator("#maxCustomers").FillAsync(greenMax.ToString());
        await Page.GetButton("Update Tour").ClickAsync();

        await Page.GetButton("Cancel").ClickAsync();
        await NavigateToAsync("/tours");

        // City Highlights should now show green badge with "3 spots"
        var cityRowGreen = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await Expect(cityRowGreen.Locator("span.badge.bg-success")).ToContainTextAsync("3 spots");

        // Verify details page shows "3 spots available"
        await cityRowGreen.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading("City Highlights")).ToBeVisibleAsync();
        var capacityGreen = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityGreen.Locator("span.badge.bg-success")).ToContainTextAsync("3 spots available");

        // === Step 5: Edit tour to create "Below Min" (yellow) state ===
        // Set MinCustomers higher than currentCount
        await Page.GetLink("Edit Tour").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        await Page.Locator("#minCustomers").FillAsync((currentCount + 5).ToString());
        await Page.Locator("#maxCustomers").FillAsync("20");
        await Page.GetButton("Update Tour").ClickAsync();

        await Page.GetButton("Cancel").ClickAsync();
        await NavigateToAsync("/tours");

        // City Highlights should now show yellow "Below Min" badge
        var cityRowYellow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await Expect(cityRowYellow.Locator("span.badge.bg-warning")).ToContainTextAsync("Below Min");

        // Verify details page shows "Below Minimum"
        await cityRowYellow.GetLink("View").ClickAsync();
        await Expect(Page.GetHeading("City Highlights")).ToBeVisibleAsync();
        var capacityYellow = Page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacityYellow.Locator("span.badge.bg-warning")).ToContainTextAsync("Below Minimum");
    }
}
