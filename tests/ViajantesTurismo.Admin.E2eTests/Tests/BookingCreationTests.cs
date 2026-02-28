using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingCreationTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_From_Customer_Details_With_Prefilled_Data()
    {
        // Navigate to customers list and find Elena Rodriguez (EBike preference)
        await NavigateToAsync("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        var elenaRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "Elena Rodriguez" });
        await elenaRow.First.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Customer Details");
        await Expect(Page.GetByText("Elena Rodriguez").First).ToBeVisibleAsync();

        // Click "Add Booking" to show the inline booking creation form
        await Page.GetButton("Add Booking").ClickAsync();
        await Expect(Page.GetByText("Create New Booking")).ToBeVisibleAsync();

        var bookingForm = Page.Locator("form:has(button:text('Create Booking'))");
        await Expect(bookingForm).ToBeVisibleAsync();

        // Verify bike type is pre-filled with Elena's preference (EBike)
        var bikeTypeSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Bike Type" }).First.Locator("select");
        await Expect(bikeTypeSelect).ToHaveValueAsync("EBike");

        // No customer select should exist (customer is pre-determined)
        var customerFields = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" });
        await Expect(customerFields).ToHaveCountAsync(0);

        // Select Cultural Experience tour (label includes dynamic date, so find the matching option)
        var tourSelect = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Tour" }).First.Locator("select");
        var culturalOption = tourSelect.Locator("option", new LocatorLocatorOptions { HasText = "Cultural Experience" });
        var optionValue = await culturalOption.GetAttributeAsync("value");
        await tourSelect.SelectOptionAsync(optionValue!);

        // Tour availability info should appear
        await Expect(bookingForm.GetByText("available")).ToBeVisibleAsync();

        // Price breakdown card should appear after selecting a tour
        await Expect(bookingForm.GetByText("Price Breakdown")).ToBeVisibleAsync();

        // Add notes
        await bookingForm.Locator("#notes").FillAsync("E2E test booking from customer details");

        // Submit the booking
        await bookingForm.GetButton("Create Booking").ClickAsync();

        // Wait for success toast
        var toast = Page.Locator(".toast");
        await Expect(toast.First).ToBeVisibleAsync();
        await Expect(toast.First).ToContainTextAsync("Booking created successfully");

        // Verify the new booking appears in the customer's bookings list
        await Expect(Page.GetByText("Cultural Experience").First).ToBeVisibleAsync();
    }
}
