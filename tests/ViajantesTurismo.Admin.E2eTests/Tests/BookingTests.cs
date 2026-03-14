using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Create_Booking_Manage_Lifecycle_Apply_Discount_And_Record_Payments()
    {
        // Arrange
        var tour = await ApiTestHelper.CreateTourAsync(ApiClient, currency: CurrencyDto.UsDollar);
        var customer = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";
        var bookingWorkflow = new BookingWorkflow(Page, NavigateToAsync);

        // Act
        var createdBookingId = await bookingWorkflow.CreateFromTourDetails(tour, customerFullName, customerSelectionLabel);

        // Verify booking details
        await bookingWorkflow.NavigateToDetails(createdBookingId);
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();

        await bookingWorkflow.ApplyDiscount(createdBookingId);

        // Assert
        await bookingWorkflow.NavigateToDetails(createdBookingId);
        await Expect(Page.GetByText("10").First).ToBeVisibleAsync(); // Discount percentage

        await bookingWorkflow.ConfirmBooking(createdBookingId);
        await bookingWorkflow.RecordPayment();

        // Verify payment appears in the payments list
        var paymentsTable = Page.Locator("table").Filter(new LocatorFilterOptions { HasText = "Cash" });
        await Expect(paymentsTable.First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();

        await bookingWorkflow.CompleteBooking();

        await Expect(Page.GetByText("completed").First).ToBeVisibleAsync();

        await bookingWorkflow.NavigateToDetails(createdBookingId);
        await Page.ReloadAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,000.00").First).ToBeVisibleAsync();
    }
}
