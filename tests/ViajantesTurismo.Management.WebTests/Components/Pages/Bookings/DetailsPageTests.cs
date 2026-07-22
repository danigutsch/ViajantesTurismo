using Microsoft.AspNetCore.Components;
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

    [Fact]
    public async Task Generating_a_contract_draft_navigates_to_its_details_route()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        var document = DocumentDetailsTestData.Create(DocumentStatusDto.DraftGenerated) with
        {
            BookingId = booking.Id
        };
        _fakeBookingsApi.AddBooking(booking);
        _fakeDocumentsApi.GeneratedDocument = document;
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/bookings/{booking.Id}");
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Act
        var generateButton = cut.FindAll("button").ShouldHaveSingleItem(button =>
            button.TextContent.Contains("Generate contract draft", StringComparison.Ordinal));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        navigationManager.Uri.ShouldBe($"http://localhost/documents/{document.Id}");
    }

    [Fact]
    public async Task Generation_failure_shows_an_alert_and_stays_on_the_booking()
    {
        // Arrange
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        _fakeBookingsApi.AddBooking(booking);
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo($"/bookings/{booking.Id}");
        var cut = Render<Details>(parameters => parameters.Add(page => page.Id, booking.Id));
        await cut.WaitForAssertionAsync(() => cut.Find("h1"));

        // Act
        var generateButton = cut.FindAll("button").ShouldHaveSingleItem(button =>
            button.TextContent.Contains("Generate contract draft", StringComparison.Ordinal));
        await cut.InvokeAsync(() => generateButton.Click());

        // Assert
        navigationManager.Uri.ShouldBe($"http://localhost/bookings/{booking.Id}");
        cut.Find("[role='alert']").TextContent.ShouldContain(
            "The contract draft could not be generated.",
            StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("No generated document was configured.", StringComparison.Ordinal);
    }

    [Fact]
    public void Contract_draft_action_is_hidden_from_an_operator()
    {
        // Arrange
        using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        authorization.SetAuthorized("operator@example.test", AuthorizationState.Authorized);
        authorization.SetRoles("Operator");
        var bookingsApi = new FakeBookingsApiClient();
        var booking = BuildBookingDto(status: BookingStatusDto.Confirmed);
        bookingsApi.AddBooking(booking);
        context.Services.AddSingleton<IBookingsApiClient>(bookingsApi);
        context.Services.AddSingleton<IDocumentsApiClient>(new FakeDocumentsApiClient());

        // Act
        var cut = context.Render<Details>(parameters => parameters.Add(page => page.Id, booking.Id));
        cut.WaitForAssertion(() => cut.Find("h1"));

        // Assert
        cut.FindAll("button").ShouldNotContain(button =>
            button.TextContent.Contains("Generate contract draft", StringComparison.Ordinal));
    }
}
