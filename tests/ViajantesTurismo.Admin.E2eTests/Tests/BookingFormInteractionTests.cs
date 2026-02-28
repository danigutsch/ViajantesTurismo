using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingFormInteractionTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Toggle_Companion_Fields_By_Room_Type()
    {
        // Navigate to Cultural Experience tour details
        await NavigateToAsync("/tours");
        var tourRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "Cultural Experience" });
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Click "Add Booking" to show the form
        await Page.GetButton("Add Booking").ClickAsync();
        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Default room type is DoubleOccupancy → companion select should be visible
        var roomTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Room Type" }).First.Locator("select");
        await Expect(roomTypeSelect).ToHaveValueAsync("DoubleOccupancy");

        var companionField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Companion (Optional)" }).First;
        await Expect(companionField.Locator("select")).ToBeVisibleAsync();

        // Select a customer to enable interactions
        var customerField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" }).First;
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = "Alice Smith (alice@example.com)" });

        // Select a companion
        await companionField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = "Carla Santos (carla@example.com)" });

        // Companion bike field should appear after selecting a companion
        var companionBikeField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Companion Bike" });
        await Expect(companionBikeField.Locator("select")).ToBeVisibleAsync();
        // Companion bike auto-filled to Carla's preference (Regular)
        await Expect(companionBikeField.Locator("select")).ToHaveValueAsync("Regular");

        // Switch to SingleOccupancy → companion fields should disappear
        await roomTypeSelect.SelectOptionAsync("SingleOccupancy");

        // Companion dropdown should no longer be visible
        await Expect(companionField.Locator("select")).Not.ToBeVisibleAsync();
        await Expect(companionBikeField.Locator("select")).Not.ToBeVisibleAsync();

        // Single Occupancy badge should appear
        await Expect(bookingForm.Locator(".badge.bg-info")).ToContainTextAsync("Single Occupancy");

        // Switch back to DoubleOccupancy → companion dropdown reappears
        await roomTypeSelect.SelectOptionAsync("DoubleOccupancy");
        await Expect(companionField.Locator("select")).ToBeVisibleAsync();

        // Companion should be cleared (no selection) after toggling
        await Expect(companionField.Locator("select")).ToHaveValueAsync("");

        // Companion bike field should NOT be visible (since companion was cleared)
        await Expect(companionBikeField.Locator("select")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_See_Live_Price_Breakdown_During_Booking_Creation()
    {
        // Navigate to Cultural Experience tour details
        // Cultural Experience: Base $1800, SingleSupplement $350, RegularBike $120, EBike $220
        await NavigateToAsync("/tours");
        var tourRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "Cultural Experience" });
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Click "Add Booking" → tour is pre-selected → price breakdown shows immediately
        await Page.GetButton("Add Booking").ClickAsync();
        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Price breakdown card should be visible since tour is pre-selected
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();

        // Initial: DoubleOccupancy, BikeType=None → Subtotal = $1,800.00
        await Expect(bookingForm.GetByText("$ 1,800.00").First).ToBeVisibleAsync();

        // Select customer Alice Smith → BikeType auto-fills to Regular → Subtotal = $1,800 + $120 = $1,920.00
        var customerField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" }).First;
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = "Alice Smith (alice@example.com)" });

        await Expect(bookingForm.GetByText("$ 1,920.00").First).ToBeVisibleAsync();

        // Change bike type to EBike → Subtotal = $1,800 + $220 = $2,020.00
        var bikeTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Principal Customer Bike" }).Locator("select");
        await bikeTypeSelect.SelectOptionAsync("EBike");
        await Expect(bookingForm.GetByText("$ 2,020.00").First).ToBeVisibleAsync();

        // Switch to SingleOccupancy → Subtotal = $1,800 + $350 + $220 = $2,370.00
        var roomTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Room Type" }).First.Locator("select");
        await roomTypeSelect.SelectOptionAsync("SingleOccupancy");
        await Expect(bookingForm.GetByText("$ 2,370.00").First).ToBeVisibleAsync();

        // Final Total should match Subtotal (no discount applied)
        var finalTotalRow = bookingForm.Locator("dd.fw-bold");
        await Expect(finalTotalRow).ToContainTextAsync("$ 2,370.00");

        // Apply percentage discount: 10% → Discount = $237.00, FinalTotal = $2,133.00
        await bookingForm.Locator("#discountType").SelectOptionAsync("Percentage");
        await bookingForm.Locator("#discountAmount").FillAsync("10");
        // Blur the input to trigger Blazor's onchange event for InputNumber
        await bookingForm.Locator("#discountAmount").BlurAsync();

        // Discount line should appear with red text
        await Expect(bookingForm.Locator(".text-danger").Filter(
            new LocatorFilterOptions { HasText = "Discount" })).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("-$ 237.00").First).ToBeVisibleAsync();

        // Final total should update
        await Expect(finalTotalRow).ToContainTextAsync("$ 2,133.00");
    }
}
