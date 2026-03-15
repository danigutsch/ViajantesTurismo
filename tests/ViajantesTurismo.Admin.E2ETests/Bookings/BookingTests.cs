using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_And_Show_Initial_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";

        // Act
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);
        await BookingWorkflow.NavigateToDetails(createdBookingId);

        // Assert
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Apply_Discount_Confirm_Booking_And_Record_Payment()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
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
    public async Task Can_Complete_Booking_And_Persist_Final_State()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
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
