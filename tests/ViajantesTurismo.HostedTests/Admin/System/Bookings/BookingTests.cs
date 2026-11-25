using SharedKernel.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using SharedKernel.Testing.Playwright;

namespace ViajantesTurismo.HostedTests.Admin.System.Bookings;

[TestScope(TestScope.System)]
public class BookingTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Can_create_booking_and_show_initial_details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = "USD" });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";

        // Act
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(
            tour,
            customer,
            MoneyText.Of(1300.00m, "USD"));
        await BookingWorkflow.NavigateToDetails(createdBookingId);

        // Assert
        ApiBaseUri.IsLoopback.ShouldBeTrue();
        var assertionTarget1 = ApiBaseUri.Port > 0;
        assertionTarget1.ShouldBeTrue();
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(MoneyText.Of(1300.00m, "USD")).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_apply_discount_confirm_booking_and_approve_receipt_credit()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = "EUR" });
        var customer = await ApiClient.CreateCustomer();
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(
            tour,
            customer,
            MoneyText.Of(1300.00m, "EUR"));

        // Act
        await BookingWorkflow.ApplyDiscount(createdBookingId);
        await BookingWorkflow.ConfirmBooking(createdBookingId);
        await ApiClient.RecordAndApproveReceiptCredit(createdBookingId, 1000m);

        // Assert
        await BookingWorkflow.NavigateToDetails(createdBookingId);
        var financialDetails = Page.Locator(".card").Filter(new LocatorFilterOptions { HasText = "Financial Details" });
        await Expect(financialDetails.GetByText(
                "Reason: E2E test discount applied for loyal customer testing",
                new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Expect(financialDetails.GetByText(MoneyText.Of(1170.00m, "EUR"), new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await BookingWorkflow.NavigateToFinances(createdBookingId);
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameRegex = new global::System.Text.RegularExpressions.Regex("^Receipt ") })).ToHaveCountAsync(1);
        await Expect(Page.GetByText("Method: Cash.", new PageGetByTextOptions { Exact = false })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_complete_booking_and_persist_final_state()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = "EUR" });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var booking = await ApiClient.CreateConfirmedPaidBooking(tour.Id, customer.Id);
        var expectedTotal = MoneyText.Of(booking.TotalPrice, "EUR");
        await BookingWorkflow.NavigateToEdit(booking.Id);
        await Expect(Page.GetButton("Complete Booking")).ToBeEnabledAsync();

        // Act
        await BookingWorkflow.CompleteBooking();
        await BookingWorkflow.NavigateToDetails(booking.Id);
        await Page.ReloadAsync();

        // Assert
        await Page.ShouldHaveTitle("Booking Details");
        var generalInformation = Page.Locator(".card")
            .Filter(new LocatorFilterOptions { HasText = "General Information" });
        await Expect(generalInformation.GetByText(
                "Completed",
                new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(expectedTotal).First).ToBeVisibleAsync();
    }
}
