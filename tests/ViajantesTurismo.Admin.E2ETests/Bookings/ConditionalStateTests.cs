namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class ConditionalStateTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_Edit_Disables_Locked_Fields_When_Bookings_Exist()
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
    public async Task Booking_Edit_Disables_All_Fields_For_Terminal_States()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var cancelledCustomer = await ApiClient.CreateCustomer();
        var completedCustomer = await ApiClient.CreateCustomer();

        var cancelledBooking = await ApiClient.CreateCancelledBooking(tour.Id, cancelledCustomer.Id);
        var completedBooking = await ApiClient.CreateCompletedBooking(tour.Id, completedCustomer.Id);

        // Act
        await ExpectTerminalBookingEditState(cancelledBooking.Id, "cancelled");

        // Assert
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await ExpectTerminalBookingEditState(completedBooking.Id, "completed");
    }

    [Fact]
    public async Task Booking_Edit_Keeps_Editable_Fields_Enabled_For_Pending_Bookings()
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

    private async Task ExpectTerminalBookingEditState(Guid bookingId, string terminalStateText)
    {
        await BookingWorkflow.NavigateToEdit(bookingId);

        await Expect(Page.Locator(".alert-warning")).ToContainTextAsync(terminalStateText);
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeDisabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeDisabledAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();
    }
}
