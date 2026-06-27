using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class BookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Can_create_booking_and_show_initial_details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";

        // Act
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);
        await BookingWorkflow.NavigateToDetails(createdBookingId);

        // Assert
        Assert.True(ApiBaseUri.IsLoopback);
        Assert.True(ApiBaseUri.Port > 0);
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_apply_discount_confirm_booking_and_record_payment()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);

        // Act
        await BookingWorkflow.ApplyDiscount(createdBookingId);
        await BookingWorkflow.ConfirmBooking(createdBookingId);
        await BookingWorkflow.RecordPayment();

        // Assert
        await BookingWorkflow.NavigateToDetails(createdBookingId);
        await Expect(Page.GetByText("10").First).ToBeVisibleAsync();
        var paymentsTable = Page.Locator("table").Filter(new LocatorFilterOptions { HasText = "Cash" });
        await Expect(paymentsTable.First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_complete_booking_and_persist_final_state()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);

        // Act
        await BookingWorkflow.ApplyDiscount(createdBookingId);
        await BookingWorkflow.ConfirmBooking(createdBookingId);
        await BookingWorkflow.RecordPayment();
        await BookingWorkflow.CompleteBooking();
        await BookingWorkflow.NavigateToDetails(createdBookingId);
        await Page.ReloadAsync();

        // Assert
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        await Expect(Page.GetByText("completed").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();
    }
}
