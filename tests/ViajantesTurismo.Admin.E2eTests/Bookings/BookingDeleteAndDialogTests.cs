using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingDeleteAndDialogTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Delete_Booking_Via_Confirm_Dialog()
    {
        // Create own tour, customer, and pending booking via API
        var api = ApiClient;
        var tour = await api.CreateTour();
        var customer = await api.CreateCustomer();
        var booking = await api.CreateBooking(tour.Id, customer.Id);

        // Navigate directly to the created booking's edit page
        await NavigateTo($"/bookings/{booking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Delete button should be visible
        var deleteButton = Page.GetButton("Delete Booking");
        await Expect(deleteButton).ToBeVisibleAsync();

        // Click Delete — confirm dialogue appears
        await deleteButton.ClickAsync();
        var dialog = Page.Locator(".modal.show");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.Locator(".modal-title")).ToContainTextAsync("Delete Booking");
        await Expect(dialog.GetByText("cannot be undone")).ToBeVisibleAsync();

        // Click "No" — booking should remain
        await dialog.GetButton("No").ClickAsync();
        await Expect(dialog).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();

        // Click Delete again and confirm this time
        await deleteButton.ClickAsync();
        var dialog2 = Page.Locator(".modal.show");
        await Expect(dialog2).ToBeVisibleAsync();
        await dialog2.GetButton("Yes, Delete").ClickAsync();

        // Should redirect to /bookings
        await Expect(Page).ToHaveURLAsync(new Regex("/bookings$"));
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Deleted booking should not appear in the list
        var deletedLink = Page.Locator($"a[href='/bookings/{booking.Id}']");
        await Expect(deletedLink).ToHaveCountAsync(0);
    }
}
