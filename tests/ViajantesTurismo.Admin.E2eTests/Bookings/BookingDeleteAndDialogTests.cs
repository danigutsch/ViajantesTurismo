using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingDeleteAndDialogTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Delete_Booking_Via_Confirm_Dialog()
    {
        // Arrange
        var api = ApiClient;
        var tour = await api.CreateTour();
        var customer = await api.CreateCustomer();
        var booking = await api.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/bookings/{booking.Id}/edit");
        var deleteButton = Page.GetButton("Delete Booking");
        await deleteButton.ClickAsync();

        // Assert
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
        await Expect(deleteButton).ToBeVisibleAsync();
        var dialog = Page.Locator(".modal.show");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.Locator(".modal-title")).ToContainTextAsync("Delete Booking");
        await Expect(dialog.GetByText("cannot be undone")).ToBeVisibleAsync();

        // Act
        await dialog.GetButton("No").ClickAsync();

        // Assert
        await Expect(dialog).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();

        // Act
        await deleteButton.ClickAsync();
        var dialog2 = Page.Locator(".modal.show");
        await Expect(dialog2).ToBeVisibleAsync();
        await dialog2.GetButton("Yes, Delete").ClickAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(new Regex("/bookings$"));
        await Expect(Page).ToHaveTitleAsync("Bookings");
        var deletedLink = Page.Locator($"a[href='/bookings/{booking.Id}']");
        await Expect(deletedLink).ToHaveCountAsync(0);
    }
}
