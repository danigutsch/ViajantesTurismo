namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingEditStateTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Cancelled_Booking_Edit_Hides_Payment_And_Action_Buttons()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateCancelledBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);

        // Assert
        await ExpectWarningContains("cancelled", "cannot be modified");
        await Expect(Page.GetButton("Record Payment")).Not.ToBeVisibleAsync();
        await ExpectPaymentsSummaryVisible();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Complete Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Confirmed_Booking_Edit_Shows_Action_Buttons_And_Payment_Section()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);

        // Assert
        await Expect(Page.Locator(".alert-warning")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Complete Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Cancel Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await ExpectPaymentsSummaryVisible();
    }

    private async Task ExpectWarningContains(params string[] expectedTexts)
    {
        var warning = Page.Locator(".alert-warning");
        foreach (var expectedText in expectedTexts)
        {
            await Expect(warning).ToContainTextAsync(expectedText);
        }
    }

    private async Task ExpectPaymentsSummaryVisible()
    {
        var paymentsCard = Page.Locator(".card").Filter(new LocatorFilterOptions { HasText = "Payments" });
        await Expect(paymentsCard.GetByText("Total Price", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Amount Paid", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Remaining Balance", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }
}
