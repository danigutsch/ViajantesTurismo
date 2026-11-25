using Microsoft.AspNetCore.Components.QuickGrid;
using SharedKernel.Testing;
using ViajantesTurismo.Management.Web.Components.Pages.Bookings;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

/// <summary>Checks the bounded owner booking grid at its typed client seam.</summary>
[TestScope(TestScope.Component)]
public sealed class BookingHistoryTests : BunitContext
{
    private readonly FakeBookingsApiClient api = new();

    public BookingHistoryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IBookingsApiClient>(api);
        Services.AddSingleton<SharedKernel.Blazor.IUiFailureReporter>(new RecordingUiFailureReporter());
    }

    [Theory]
    [InlineData(true, "Customer", BookingPageSort.CustomerName)]
    [InlineData(false, "Tour", BookingPageSort.TourName)]
    public async Task Only_the_owner_window_uses_the_requested_page_and_sort(bool byTour, string title, BookingPageSort sort)
    {
        // Arrange
        var owner = Guid.NewGuid();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rows = Enumerable.Range(0, 23).Select(index => BuildBookingDto(
            tourId: byTour ? owner : null,
            customerId: !byTour && index % 2 == 0 ? owner : null,
            companionId: !byTour && index % 2 != 0 ? owner : null,
            tourName: $"History Tour {index:D2}",
            customerName: $"History Customer {index:D2}",
            bookingDate: start.AddDays(index))).ToArray();
        foreach (var row in rows)
        {
            api.AddBooking(row);
        }

        var unrelated = BuildBookingDto();
        api.AddBooking(unrelated);
        var cut = Render<BookingHistory>(parameters => parameters
            .Add(page => page.OwnerId, owner)
            .Add(page => page.IsTourHistory, byTour));
        cut.WaitForElement($"a[href='/bookings/{rows[^1].Id}']");
        var initialMarkup = cut.Markup;
        var initial = api.HistoryRequests.ShouldHaveSingleItem();
        var pagination = cut.FindComponent<Paginator>().Instance.State;

        // Act
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(2));
        cut.WaitForElement($"a[href='/bookings/{rows[0].Id}']");
        var third = api.HistoryRequests[^1];
        var column = cut.FindComponents<TemplateColumn<GetBookingDto>>()
            .ShouldHaveSingleItem(candidate => candidate.Instance.Title == title).Instance;
        var grid = cut.FindComponent<QuickGrid<GetBookingDto>>().Instance;
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Descending));
        var (requestOwner, requestByTour, _, requestSort, descending) = api.HistoryRequests[^1];
        await cut.InvokeAsync(() => grid.SortByColumnAsync(column, SortDirection.Ascending));
        var (_, _, _, ascendingSort, ascendingDescending) = api.HistoryRequests[^1];

        // Assert
        initial.ShouldBe((owner, byTour, 1, BookingPageSort.Default, false));
        third.ShouldBe((owner, byTour, 3, BookingPageSort.Default, false));
        requestOwner.ShouldBe(owner);
        requestByTour.ShouldBe(byTour);
        requestSort.ShouldBe(sort);
        descending.ShouldBeTrue();
        ascendingSort.ShouldBe(sort);
        ascendingDescending.ShouldBeFalse();
        initialMarkup.ShouldNotContain($"/bookings/{rows[0].Id}", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain($"/bookings/{unrelated.Id}", StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_failed_later_page_removes_the_previously_rendered_bookings()
    {
        // Arrange
        var owner = Guid.NewGuid();
        for (var index = 0; index < 15; index++)
        {
            api.AddBooking(BuildBookingDto(tourId: owner));
        }

        var cut = Render<BookingHistory>(parameters => parameters.Add(page => page.OwnerId, owner)
            .Add(page => page.IsTourHistory, true));
        cut.WaitForElement("tbody tr");
        var pagination = cut.FindComponent<Paginator>().Instance.State;
        api.SetGetBookingHistoryOutcome(FakeApiClientOutcomes.TransportFailure<GetBookingHistoryPageDto>());

        // Act
        await cut.InvokeAsync(() => pagination.SetCurrentPageIndexAsync(1));

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe(
            "We couldn't load bookings right now. Please try again."));
        cut.FindAll("tbody tr").ShouldBeEmpty();
    }

}
