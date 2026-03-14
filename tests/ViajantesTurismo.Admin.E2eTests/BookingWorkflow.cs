using System.Globalization;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests;

internal sealed class BookingWorkflow(IPage page, Func<string, Task> navigateTo)
{
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

        var toast = page.Locator(".toast");
        await toast.First.WaitForAsync();

        var createdBookingRow = page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = customerFullName });
        await createdBookingRow.First.WaitForAsync();

        var bookingHref = await createdBookingRow.First.GetLink("View").GetAttributeAsync("href");
        Assert.NotNull(bookingHref);

        var bookingIdText = bookingHref.Split('/').Last();
        Assert.True(Guid.TryParse(bookingIdText, out var bookingId));

        return bookingId;
    }

    public async Task NavigateToDetails(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}");
        Assert.Equal("Booking Details", await page.TitleAsync());
    }

    public async Task ApplyDiscount(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}/edit");
        Assert.Equal("Edit Booking", await page.TitleAsync());

        await page.Locator("#notes").FillAsync("E2E test booking - notes updated during edit");
        await page.SelectOptionAsync("#discountType", "Percentage");
        await page.FillAsync("#discountAmount", "10");
        await page.FillAsync("#discountReason", "E2E test discount applied for loyal customer testing");

        await page.GetButton("Update Booking").ClickAsync();

        var successAlert = page.Locator(".alert-success");
        await successAlert.WaitForAsync();
        Assert.Contains("Booking updated successfully!", await successAlert.InnerTextAsync(), StringComparison.Ordinal);

        var cancelRedirect = page.Locator(".alert-info button", new PageLocatorOptions { HasText = "Cancel" });
        if (await cancelRedirect.CountAsync() > 0)
        {
            await cancelRedirect.ClickAsync();
        }
    }

    public async Task ConfirmBooking(Guid bookingId)
    {
        await navigateTo($"/bookings/{bookingId}/edit");
        Assert.Equal("Edit Booking", await page.TitleAsync());

        await page.GetButton("Confirm Booking").ClickAsync();

        var confirmToast = page.Locator(".toast");
        await confirmToast.First.WaitForAsync();
        Assert.Contains("Booking confirmed successfully", await confirmToast.First.InnerTextAsync(), StringComparison.Ordinal);
        await page.GetButton("Complete Booking").WaitForAsync();
    }

    public async Task RecordPayment()
    {
        await page.GetButton("Record Payment").ClickAsync();

        var paymentCard = page.Locator(".card.border-success");
        await paymentCard.WaitForAsync();

        await paymentCard.Locator("#amount").FillAsync("1000");
        await paymentCard.Locator("#paymentDate").FillAsync(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        await paymentCard.Locator("#method").SelectOptionAsync("Cash");

        await paymentCard.GetButton("Record Payment").ClickAsync();

        var paymentToast = page.Locator(".toast")
            .Filter(new LocatorFilterOptions { HasText = "Payment recorded successfully" });
        await paymentToast.First.WaitForAsync();
        Assert.Contains("Payment recorded successfully", await paymentToast.First.InnerTextAsync(), StringComparison.Ordinal);
        await paymentToast.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
    }

    public async Task CompleteBooking()
    {
        await page.GetButton("Complete Booking").ClickAsync();

        var completeToast = page.Locator(".toast");
        await completeToast.First.WaitForAsync();
        Assert.Contains("Booking completed successfully", await completeToast.First.InnerTextAsync(), StringComparison.Ordinal);
    }
}
