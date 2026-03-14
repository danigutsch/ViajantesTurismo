using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingFormInteractionTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Toggle_Companion_Fields_By_Room_Type()
    {
        // Arrange: create owned tour and customers.
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var companion = await ApiClient.CreateCustomer();
        var customerLabel = $"{customer.FirstName} {customer.LastName} ({customer.Email})";
        var companionLabel = $"{companion.FirstName} {companion.LastName} ({companion.Email})";

        // Navigate to owned tour details
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Click "Add Booking" to show the form
        await Page.GetButton("Add Booking").ClickAsync();
        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // The default room type is DoubleOccupancy → companion select should be visible
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
            .SelectOptionAsync(new SelectOptionValue { Label = customerLabel });

        // Select a companion
        await companionField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = companionLabel });

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

        // Companion bike field should NOT be visible (since the companion was cleared)
        await Expect(companionBikeField.Locator("select")).Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_See_Live_Price_Breakdown_During_Booking_Creation()
    {
        // Arrange: create owned tour (USD) and customer.
        // Test Tour defaults: Base $1000, SingleSupplement $200, RegularBike $50, EBike $100.
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var customerLabel = $"{customer.FirstName} {customer.LastName} ({customer.Email})";

        // Navigate to owned tour details
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Click "Add Booking" → tour is pre-selected → price breakdown shows immediately
        await Page.GetButton("Add Booking").ClickAsync();
        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Price breakdown card should be visible since tour is pre-selected
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();

        // Initial: DoubleOccupancy, BikeType=None → Subtotal = $1,000.00
        await Expect(bookingForm.GetByText("$ 1,000.00").First).ToBeVisibleAsync();

        // Select customer → BikeType auto-fills to Regular → Subtotal = $1,000 + $50 = $1,050.00
        var customerField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" }).First;
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = customerLabel });

        await Expect(bookingForm.GetByText("$ 1,050.00").First).ToBeVisibleAsync();

        // Change bike type to EBike → Subtotal = $1,000 + $100 = $1,100.00
        var bikeTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Principal Customer Bike" }).Locator("select");
        await bikeTypeSelect.SelectOptionAsync("EBike");
        await Expect(bookingForm.GetByText("$ 1,100.00").First).ToBeVisibleAsync();

        // Switch to SingleOccupancy → Subtotal = $1,000 + $200 + $100 = $1,300.00
        var roomTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Room Type" }).First.Locator("select");
        await roomTypeSelect.SelectOptionAsync("SingleOccupancy");
        await Expect(bookingForm.GetByText("$ 1,300.00").First).ToBeVisibleAsync();

        // Final Total should match Subtotal (no discount applied)
        var finalTotalRow = bookingForm.Locator("dd.fw-bold");
        await Expect(finalTotalRow).ToContainTextAsync("$ 1,300.00");

        // Apply percentage discount: 10% → Discount = $130.00, FinalTotal = $1,170.00
        await bookingForm.Locator("#discountType").SelectOptionAsync("Percentage");
        await bookingForm.Locator("#discountAmount").FillAsync("10");
        // Blur the input to trigger Blazor's onchange event for InputNumber
        await bookingForm.Locator("#discountAmount").BlurAsync();

        // Discount line should appear with red text
        await Expect(bookingForm.Locator(".text-danger").Filter(
            new LocatorFilterOptions { HasText = "Discount" })).ToBeVisibleAsync();
        await Expect(bookingForm.GetByText("-$ 130.00").First).ToBeVisibleAsync();

        // Final total should update
        await Expect(finalTotalRow).ToContainTextAsync("$ 1,170.00");
    }
}
