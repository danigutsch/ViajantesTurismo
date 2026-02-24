using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Pages.Bookings;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Bookings;

public sealed class DetailsPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi = new();

    public DetailsPageTests()
    {
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
    }

    [Fact]
    public void NotFound_Hides_Edit_Booking_Link()
    {
        // Arrange — non-existent booking
        var bookingId = Guid.NewGuid();

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert — "Edit Booking" link must NOT be present
        Assert.DoesNotContain("Edit Booking", cut.Markup);
    }

    [Fact]
    public void NotFound_Shows_Back_To_List_Link()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert — should still have a way to navigate back
        var backLink = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Contains("Back to List"));
        Assert.NotNull(backLink);
        Assert.Equal("/bookings", backLink.GetAttribute("href"));
    }

    [Fact]
    public void NotFound_Shows_Alert_Message()
    {
        // Arrange
        var bookingId = Guid.NewGuid();

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, bookingId));
        cut.WaitForAssertion(() => cut.Find(".alert.alert-danger"));

        // Assert
        var alert = cut.Find(".alert.alert-danger");
        Assert.Contains("Booking not found", alert.TextContent);
    }

    [Fact]
    public void Booking_Found_Shows_Edit_Booking_Link()
    {
        // Arrange
        var booking = BuildBookingDto();
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(p => p.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        var editLink = cut.FindAll("a").FirstOrDefault(a => a.TextContent.Contains("Edit Booking"));
        Assert.NotNull(editLink);
        Assert.Equal($"/bookings/{booking.Id}/edit", editLink.GetAttribute("href"));
    }
}
