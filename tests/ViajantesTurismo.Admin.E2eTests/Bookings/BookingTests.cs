using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_Manage_Lifecycle_Apply_Discount_And_Record_Payments()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";

        // Act
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);

        // Verify booking details
        await BookingWorkflow.NavigateToDetails(createdBookingId);
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();

        await BookingWorkflow.ApplyDiscount(createdBookingId);

        // Assert
        await BookingWorkflow.NavigateToDetails(createdBookingId);
        await Expect(Page.GetByText("10").First).ToBeVisibleAsync(); // Discount percentage

        await BookingWorkflow.ConfirmBooking(createdBookingId);
        await BookingWorkflow.RecordPayment();

        // Verify payment appears in the payments list
        var paymentsTable = Page.Locator("table").Filter(new LocatorFilterOptions { HasText = "Cash" });
        await Expect(paymentsTable.First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();

        await BookingWorkflow.CompleteBooking();

        await Expect(Page.GetByText("completed").First).ToBeVisibleAsync();

        await BookingWorkflow.NavigateToDetails(createdBookingId);
        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();
    }
}
