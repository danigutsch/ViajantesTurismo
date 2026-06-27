using ViajantesTurismo.Management.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class DetailsPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi = new();

    public DetailsPageTests()
    {
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
    }

    [Fact]
    public void Booking_found_shows_edit_booking_link()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        var editLink = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Contains("Edit Booking", StringComparison.Ordinal));
        Assert.NotNull(editLink);
        Assert.Equal($"/bookings/{booking.Id}/edit", editLink.GetAttribute("href"));
    }
}
