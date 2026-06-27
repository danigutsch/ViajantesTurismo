namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class ConditionalStateTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_edit_disables_locked_fields_when_bookings_exist()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 10 });
        var customer = await ApiClient.CreateCustomer();
        _ = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/edittour/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        // Assert
        await Expect(Page.Locator("#identifier")).ToBeDisabledAsync();
        await Expect(Page.Locator("#currency")).ToBeDisabledAsync();
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("existing bookings");
        await Expect(Page.Locator("#name")).ToBeEnabledAsync();
        await Expect(Page.Locator("#startDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#endDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#price")).ToBeEnabledAsync();
        await Expect(Page.Locator("#services")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Booking_edit_disables_all_fields_for_terminal_states()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var cancelledCustomer = await ApiClient.CreateCustomer();
        var completedCustomer = await ApiClient.CreateCustomer();

        var cancelledBooking = await ApiClient.CreateCancelledBooking(tour.Id, cancelledCustomer.Id);
        var completedBooking = await ApiClient.CreateCompletedBooking(tour.Id, completedCustomer.Id);

        // Act
        await ConditionalStateTestHelpers.ExpectTerminalBookingEditState(Page, BookingWorkflow.NavigateToEdit, cancelledBooking.Id, "cancelled");

        // Assert
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await ConditionalStateTestHelpers.ExpectTerminalBookingEditState(Page, BookingWorkflow.NavigateToEdit, completedBooking.Id, "completed");
    }

    [Fact]
    public async Task Booking_edit_keeps_editable_fields_enabled_for_pending_bookings()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var pendingCustomer = await ApiClient.CreateCustomer();
        var pendingBooking = await ApiClient.CreateBooking(tour.Id, pendingCustomer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(pendingBooking.Id);

        // Assert
        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();
    }
}
