namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class ConditionalStateTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_Edit_Disables_Locked_Fields_When_Bookings_Exist()
    {
        // Arrange: create owned tour/customer/booking so the tour has bookings.
        var tour = await ApiClient.CreateTour(minCustomers: 1, maxCustomers: 10);
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);
        _ = await ApiClient.ConfirmBooking(booking.Id);

        // Act: navigate directly to the owned tour edit page.
        await NavigateTo($"/edittour/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        // Identifier and Currency should be disabled
        var identifier = Page.Locator("#identifier");
        await Expect(identifier).ToBeDisabledAsync();

        var currency = Page.Locator("#currency");
        await Expect(currency).ToBeDisabledAsync();

        // Warning alert should be visible
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("existing bookings");

        // Non-locked fields should remain editable
        await Expect(Page.Locator("#name")).ToBeEnabledAsync();
        await Expect(Page.Locator("#startDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#endDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#price")).ToBeEnabledAsync();
        await Expect(Page.Locator("#services")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Booking_Edit_Disables_All_Fields_For_Terminal_States()
    {
        // Arrange: create owned bookings in Cancelled, Completed, and Pending states.
        var tour = await ApiClient.CreateTour(minCustomers: 1, maxCustomers: 20);
        var cancelledCustomer = await ApiClient.CreateCustomer();
        var completedCustomer = await ApiClient.CreateCustomer();
        var pendingCustomer = await ApiClient.CreateCustomer();

        var cancelledBooking = await ApiClient.CreateBooking(tour.Id, cancelledCustomer.Id);
        _ = await ApiClient.CancelBooking(cancelledBooking.Id);

        var completedBooking = await ApiClient.CreateBooking(tour.Id, completedCustomer.Id);
        _ = await ApiClient.ConfirmBooking(completedBooking.Id);
        _ = await ApiClient.CompleteBooking(completedBooking.Id);

        var pendingBooking = await ApiClient.CreateBooking(tour.Id, pendingCustomer.Id);

        // === Cancelled booking: all form fields disabled ===
        await NavigateTo($"/bookings/{cancelledBooking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Warning alert visible
        var cancelledWarning = Page.Locator(".alert-warning");
        await Expect(cancelledWarning).ToContainTextAsync("cancelled");

        // Form inputs disabled
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeDisabledAsync();

        // Update button disabled
        await Expect(Page.GetButton("Update Booking")).ToBeDisabledAsync();

        // Delete button still enabled
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();

        // Cancel and Confirm action buttons should NOT be visible
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();

        // === Completed booking: same disabled behavior ===
        await NavigateTo($"/bookings/{completedBooking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Warning alert visible
        var completedWarning = Page.Locator(".alert-warning");
        await Expect(completedWarning).ToContainTextAsync("completed");

        // Form inputs disabled
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeDisabledAsync();

        // Update button disabled
        await Expect(Page.GetButton("Update Booking")).ToBeDisabledAsync();

        // Delete button still enabled
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();

        // === Pending booking: fields should be ENABLED ===
        await NavigateTo($"/bookings/{pendingBooking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();
    }
}
