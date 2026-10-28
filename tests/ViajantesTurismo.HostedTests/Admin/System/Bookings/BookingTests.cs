using PandaBear.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Bases;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using PandaBear.Testing.Playwright;
using System.Globalization;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

[TestScope(TestScope.System)]
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
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(
            tour,
            customerFullName,
            customerSelectionLabel,
            "$ 1,300.00");
        await BookingWorkflow.NavigateToDetails(createdBookingId);

        // Assert
        ApiBaseUri.IsLoopback.ShouldBeTrue();
        var assertionTarget1 = ApiBaseUri.Port > 0;
        assertionTarget1.ShouldBeTrue();
        await Expect(Page.GetByText("Pending").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Unpaid").First).ToBeVisibleAsync();
        await Expect(Page.GetByText(tour.Name).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(customerFullName).First).ToBeVisibleAsync();
        await Expect(Page.GetByText("$ 1,300.00").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_apply_discount_confirm_booking_and_approve_receipt_credit()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.Euro });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var customerSelectionLabel = $"{customerFullName} ({customer.Email})";
        var createdBookingId = await BookingWorkflow.CreateFromTourDetails(
            tour,
            customerFullName,
            customerSelectionLabel,
            "1,300.00 €");

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
        await Expect(financialDetails.GetByText("1,170.00 €", new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await BookingWorkflow.NavigateToFinances(createdBookingId);
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { NameRegex = new System.Text.RegularExpressions.Regex("^Receipt ") })).ToHaveCountAsync(1);
        await Expect(Page.GetByText("Method: Cash.", new PageGetByTextOptions { Exact = false })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_complete_booking_and_persist_final_state()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.Euro });
        var customer = await ApiClient.CreateCustomer();
        var customerFullName = $"{customer.FirstName} {customer.LastName}";
        var booking = await ApiClient.CreateConfirmedPaidBooking(tour.Id, customer.Id);
        var expectedTotal = $"{booking.TotalPrice.ToString("N2", CultureInfo.InvariantCulture)} €";
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
