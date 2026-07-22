using System.Globalization;
using ViajantesTurismo.Admin.Contracts.Application;
using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Workflows;

/// <summary>
/// Encapsulates reusable booking-focused browser workflows for E2E tests.
/// Keeps navigation, form interaction, and toast handling out of test bodies
/// while leaving scenario assertions visible in the tests themselves.
/// </summary>
/// <param name="page">The active Playwright page.</param>
/// <param name="navigateTo">Navigation function that resolves relative application routes.</param>
internal sealed class BookingWorkflow(IPage page, Func<string, Task> navigateTo)
{
    private UiFeedbackAssertions UiFeedback => new(page);

    /// <summary>
    /// Navigates directly to the booking edit page for a known booking identifier.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task NavigateToEdit(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}/edit");
        await Expect(page).ToHaveTitleAsync("Edit Booking");
    }

    /// <summary>
    /// Creates a booking from the tour details page and returns the created booking identifier.
    /// </summary>
    /// <param name="tour">The tour that will receive the new booking.</param>
    /// <param name="customerFullName">The owned test customer full name used to identify the new row.</param>
    /// <param name="customerSelectionLabel">The select option label shown in the booking form.</param>
    /// <param name="expectedFinalTotal">The rendered total that proves the server processed the selected options.</param>
    /// <returns>The identifier of the created booking.</returns>
    public async Task<Guid> CreateFromTourDetails(
        GetTourDto tour,
        string customerFullName,
        string customerSelectionLabel,
        string expectedFinalTotal)
    {
        await navigateTo($"/tours/{tour.Id}");
        await Expect(page).ToHaveTitleAsync("Tour Details");
        await page.GetByText(tour.Name).First.WaitForAsync();

        var addBookingButton = page.GetButton("Add Booking");
        await addBookingButton.WaitForAsync();
        await addBookingButton.ClickAsync();

        var bookingForm = page.Locator("form:has(button:text('Create Booking'))");
        await bookingForm.WaitForAsync();
        await bookingForm.GetButton("Create Booking").WaitForAsync();

        var customerField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Customer" }).First;
        await customerField.Locator("select")
            .SelectOptionAsync(new SelectOptionValue { Label = customerSelectionLabel });

        var roomTypeField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Room Type" }).First;
        await roomTypeField.Locator("select").SelectOptionAsync("SingleOccupancy");

        var bikeTypeField = bookingForm.Locator("div.mb-3")
            .Filter(new LocatorFilterOptions { HasText = "Principal Customer Bike" });
        await bikeTypeField.Locator("select").SelectOptionAsync("EBike");

        var finalTotal = bookingForm.Locator("dt")
            .Filter(new LocatorFilterOptions { HasText = "Final Total:" })
            .Locator("xpath=following-sibling::dd[1]");
        await Expect(finalTotal).ToHaveTextAsync(expectedFinalTotal);

        await bookingForm.Locator("#notes").FillAsync("E2E test booking created from tour details");
        var toastTask = UiFeedback.ExpectToast("Booking created successfully");
        await bookingForm.GetButton("Create Booking").ClickAsync();

        await toastTask;

        var createdBookingRow = page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = customerFullName });
        await createdBookingRow.First.WaitForAsync();

        var bookingHref = await createdBookingRow.First.GetLink("View").GetAttributeAsync("href");
        _ = (bookingHref).ShouldNotBeNull();

        var bookingHrefSegments = bookingHref.Split('/');
        var bookingIdText = bookingHrefSegments[^1];
        (Guid.TryParse(bookingIdText, out var bookingId)).ShouldBeTrue();

        return bookingId;
    }

    /// <summary>
    /// Navigates directly to the booking details page for a known booking identifier.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task NavigateToDetails(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}");
        await Expect(page).ToHaveTitleAsync("Booking Details");
    }

    /// <summary>
    /// Applies the standard percentage discount used by booking workflow tests.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task ApplyDiscount(Guid bookingId)
    {
        await NavigateToEdit(bookingId);

        var discountType = page.Locator("#discountType");
        await discountType.SelectOptionAsync("Percentage");
        await discountType.BlurAsync();
        await Expect(discountType).ToHaveValueAsync("Percentage");
        await page.Locator("#discountAmount").WaitForAsync();
        var discountAmount = page.Locator("#discountAmount");
        await discountAmount.FillAsync("10");
        await discountAmount.BlurAsync();
        await Expect(discountAmount).ToHaveValueAsync("10");

        const string discountReasonText = "E2E test discount applied for loyal customer testing";
        var discountReason = page.Locator("#discountReason");
        await discountReason.FillAsync(discountReasonText);
        await discountReason.BlurAsync();
        await Expect(discountReason).ToHaveValueAsync(discountReasonText);

        var successAlert = page.GetByRole(AriaRole.Alert)
            .Filter(new LocatorFilterOptions { HasText = "Booking updated successfully!" });
        var successTask = Expect(successAlert).ToBeVisibleAsync();
        await page.GetButton("Update Booking").ClickAsync();
        await successTask;
        await NavigateToDetails(bookingId);
    }

    /// <summary>
    /// Confirms a booking from the edit page and waits for the completion action to become available.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task ConfirmBooking(Guid bookingId)
    {
        await NavigateToEdit(bookingId);

        var toastTask = UiFeedback.ExpectToast("Booking confirmed successfully");
        await page.GetButton("Confirm Booking").ClickAsync();

        await toastTask;
        await page.GetButton("Complete Booking").WaitForAsync();
    }

    /// <summary>
    /// Records the standard cash payment used by booking workflow tests.
    /// </summary>
    public async Task RecordPayment()
    {
        await page.GetButton("Record Payment").ClickAsync();

        var paymentCard = page.Locator(".card.border-success");
        await paymentCard.WaitForAsync();

        await paymentCard.Locator("#amount").FillAsync("1000");
        await paymentCard.Locator("#paymentDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await paymentCard.Locator("#method").SelectOptionAsync("Cash");

        var toastTask = UiFeedback.ExpectToastThenHide("Payment recorded successfully");
        await paymentCard.GetButton("Record Payment").ClickAsync();

        await toastTask;
    }

    /// <summary>
    /// Completes the booking from the current page and verifies the completion toast.
    /// </summary>
    public async Task CompleteBooking()
    {
        var toastTask = UiFeedback.ExpectToast("Booking completed successfully");
        await page.GetButton("Complete Booking").ClickAsync();

        await toastTask;
    }
}
