using System.Globalization;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Workflows;

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
        Assert.Equal("Edit Booking", await page.TitleAsync());
    }

    /// <summary>
    /// Creates a booking from the tour details page and returns the created booking identifier.
    /// </summary>
    /// <param name="tour">The tour that will receive the new booking.</param>
    /// <param name="customerFullName">The owned test customer full name used to identify the new row.</param>
    /// <param name="customerSelectionLabel">The select option label shown in the booking form.</param>
    /// <returns>The identifier of the created booking.</returns>
    public async Task<Guid> CreateFromTourDetails(
        GetTourDto tour,
        string customerFullName,
        string customerSelectionLabel)
    {
        await navigateTo($"/tours/{tour.Id}");
        Assert.Equal("Tour Details", await page.TitleAsync());
        await page.GetByText(tour.Name).First.WaitForAsync();

        await page.GetButton("Add Booking").ClickAsync();
        await page.GetButton("Create Booking").WaitForAsync();

        var bookingForm = page.Locator("form:has(button:text('Create Booking'))");

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

        await bookingForm.Locator("#notes").FillAsync("E2E test booking created from tour details");
        await bookingForm.GetButton("Create Booking").ClickAsync();

        await UiFeedback.ExpectToast("Booking created successfully");

        var createdBookingRow = page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = customerFullName });
        await createdBookingRow.First.WaitForAsync();

        var bookingHref = await createdBookingRow.First.GetLink("View").GetAttributeAsync("href");
        Assert.NotNull(bookingHref);

        var bookingIdText = bookingHref.Split('/').Last();
        Assert.True(Guid.TryParse(bookingIdText, out var bookingId));

        return bookingId;
    }

    /// <summary>
    /// Navigates directly to the booking details page for a known booking identifier.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task NavigateToDetails(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}");
        Assert.Equal("Booking Details", await page.TitleAsync());
    }

    /// <summary>
    /// Applies the standard percentage discount used by booking workflow tests.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task ApplyDiscount(Guid bookingId)
    {
        await NavigateToEdit(bookingId);

        await page.Locator("#notes").FillAsync("E2E test booking - notes updated during edit");
        await page.SelectOptionAsync("#discountType", "Percentage");
        await page.FillAsync("#discountAmount", "10");
        await page.FillAsync("#discountReason", "E2E test discount applied for loyal customer testing");

        await page.GetButton("Update Booking").ClickAsync();

        var successAlert = page.Locator(".alert-success");
        await successAlert.WaitForAsync();
        Assert.Contains("Booking updated successfully!", await successAlert.InnerTextAsync(), StringComparison.Ordinal);

        await page.CancelTimedRedirect();
    }

    /// <summary>
    /// Confirms a booking from the edit page and waits for the completion action to become available.
    /// </summary>
    /// <param name="bookingId">The booking identifier.</param>
    public async Task ConfirmBooking(Guid bookingId)
    {
        await NavigateToEdit(bookingId);

        await page.GetButton("Confirm Booking").ClickAsync();

        await UiFeedback.ExpectToast("Booking confirmed successfully");
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

        await paymentCard.GetButton("Record Payment").ClickAsync();

        await UiFeedback.ExpectToastThenHide("Payment recorded successfully");
    }

    /// <summary>
    /// Completes the booking from the current page and verifies the completion toast.
    /// </summary>
    public async Task CompleteBooking()
    {
        await page.GetButton("Complete Booking").ClickAsync();

        await UiFeedback.ExpectToast("Booking completed successfully");
    }
}
