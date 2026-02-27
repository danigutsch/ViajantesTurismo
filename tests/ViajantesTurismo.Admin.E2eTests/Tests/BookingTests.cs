using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_Manage_Lifecycle_Apply_Discount_And_Record_Payments()
    {
        // === Create booking from Tour Details ===
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        // Find "Cultural Experience" tour and view its details
        var tourRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "Cultural Experience" });
        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText("Cultural Experience").First).ToBeVisibleAsync();

        // Click "Add Booking" to show the booking creation form
        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetButton("Create Booking")).ToBeVisibleAsync();

        // Fill booking form: locate selects within the form area
        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");

        // Select customer: David Lee
        var customerField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" }).First;
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = "David Lee (david@example.com)" });

        // Select Room Type: Single Room
        var roomTypeField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Room Type" }).First;
        await roomTypeField.Locator("select").SelectOptionAsync("SingleOccupancy");

        // Select Bike Type: E-Bike
        var bikeTypeField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Principal Customer Bike" });
        await bikeTypeField.Locator("select").SelectOptionAsync("EBike");

        // Add notes
        await bookingForm.Locator("#notes").FillAsync("E2E test booking created from tour details");

        // Submit
        await bookingForm.GetButton("Create Booking").ClickAsync();

        // Wait for success toast
        var toast = Page.Locator(".toast");
        await Expect(toast.First).ToBeVisibleAsync();

        // === Navigate to bookings list and find the new booking ===
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        var bookingRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "David Lee" });
        await Expect(bookingRow.First).ToBeVisibleAsync();

        // Navigate to booking details
        await bookingRow.First.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        // Verify booking details
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Cultural Experience").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("David Lee").First).ToBeVisibleAsync();

        // Total price should include base + single supplement + ebike: 1800 + 350 + 220 = 2370
        await Expect(Page.GetByText("$ 2,370.00").First).ToBeVisibleAsync();

        // Extract booking ID from URL
        var bookingUrl = Page.Url;
        var bookingId = bookingUrl.Split('/').Last();

        // === Edit booking: apply discount and update notes ===
        await NavigateToAsync($"/bookings/{bookingId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Update notes
        await Page.Locator("#notes").FillAsync("E2E test booking - notes updated during edit");

        // Apply percentage discount
        await Page.SelectOptionAsync("#discountType", "Percentage");
        await Page.FillAsync("#discountAmount", "10");
        await Page.FillAsync("#discountReason", "E2E test discount applied for loyal customer testing");

        await Page.GetButton("Update Booking").ClickAsync();

        // Success alert
        var successAlert = Page.Locator(".alert-success");
        await Expect(successAlert).ToBeVisibleAsync();
        await Expect(successAlert).ToContainTextAsync("Booking updated successfully!");

        // Cancel auto-redirect
        var cancelRedirect = Page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
        if (await cancelRedirect.CountAsync() > 0)
        {
            await cancelRedirect.ClickAsync();
        }

        // === Verify discount on details ===
        await NavigateToAsync($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        await Expect(Page.GetByText("10").First).ToBeVisibleAsync(); // Discount percentage

        // === Lifecycle: Confirm booking ===
        await NavigateToAsync($"/bookings/{bookingId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        await Page.GetButton("Confirm Booking").ClickAsync();

        // Wait for toast confirmation
        var confirmToast = Page.Locator(".toast");
        await Expect(confirmToast.First).ToBeVisibleAsync();
        await Expect(confirmToast.First).ToContainTextAsync("Booking confirmed successfully");

        // "Confirm Booking" should be replaced by "Complete Booking"
        await Expect(Page.GetButton("Complete Booking")).ToBeVisibleAsync();

        // === Record payment ===
        await Page.GetButton("Record Payment").ClickAsync();

        var paymentCard = Page.Locator(".card.border-success");
        await Expect(paymentCard).ToBeVisibleAsync();

        await paymentCard.Locator("#amount").FillAsync("1000");
        await paymentCard.Locator("#paymentDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd"));
        await paymentCard.Locator("#method").SelectOptionAsync("Cash");

        await paymentCard.GetButton("Record Payment").ClickAsync();

        // Wait for payment toast and verify it
        var paymentToast = Page.Locator(".toast");
        await Expect(paymentToast.First).ToBeVisibleAsync();
        await Expect(paymentToast.First).ToContainTextAsync("Payment recorded successfully");

        // Wait for payment toast to disappear before next action
        await Expect(paymentToast.First).ToBeHiddenAsync(new LocatorAssertionsToBeHiddenOptions { Timeout = 10_000 });

        // Verify payment appears in the payments list
        var paymentsTable = Page.Locator("table").Filter(new LocatorFilterOptions { HasText = "Cash" });
        await Expect(paymentsTable.First).ToBeVisibleAsync();

        // Amount Paid should show $1,000.00
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();

        // === Complete booking ===
        await Page.GetButton("Complete Booking").ClickAsync();

        // Wait for completion toast
        var completeToast = Page.Locator(".toast");
        await Expect(completeToast.First).ToBeVisibleAsync();
        await Expect(completeToast.First).ToContainTextAsync("Booking completed successfully");

        // After completion, the booking should show as completed and fields disabled
        await Expect(Page.GetByText("completed").First).ToBeVisibleAsync();

        // === Refresh persistence: hard reload and verify completed booking survives ===
        await NavigateToAsync($"/bookings/{bookingId}");
        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        await Expect(Page.GetByText("Cultural Experience").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("David Lee").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();
    }
}
