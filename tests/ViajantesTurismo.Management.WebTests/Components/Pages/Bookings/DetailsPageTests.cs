using ViajantesTurismo.Management.Web.Components.Pages.Bookings;
using ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class DetailsPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi = new();
    private readonly FakeDocumentsApiClient _fakeDocumentsApi = new();

    public DetailsPageTests()
    {
        var authorization = AddAuthorization();
        authorization.SetAuthorized("admin@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Admin");
        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<IDocumentsApiClient>(_fakeDocumentsApi);
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
        _ = (editLink).ShouldNotBeNull();
        (editLink.GetAttribute("href")).ShouldBe($"/bookings/{booking.Id}/edit");
    }

    [Theory]
    [InlineData(BookingStatusDto.Confirmed, true)]
    [InlineData(BookingStatusDto.Completed, true)]
    [InlineData(BookingStatusDto.Pending, false)]
    [InlineData(BookingStatusDto.Cancelled, false)]
    public void Contract_draft_action_matches_booking_eligibility_for_admin(BookingStatusDto bookingStatus, bool shouldShowAction)
    {
        // Arrange
        var booking = BuildBookingDto(status: bookingStatus);
        _fakeBookingsApi.AddBooking(booking);

        // Act
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        var actionIsVisible = cut.FindAll("button").Any(button =>
            button.TextContent.Contains("Generate contract draft", StringComparison.Ordinal));
        actionIsVisible.ShouldBe(shouldShowAction);
    }
}
