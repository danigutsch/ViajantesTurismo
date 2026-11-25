using Microsoft.AspNetCore.Components.QuickGrid;
using SharedKernel.Testing;
using ViajantesTurismo.Management.WebTests.Infrastructure;
using Index = ViajantesTurismo.Management.Web.Components.Pages.Bookings.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

[TestScope(TestScope.Component)]
public class IndexPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi;
    private readonly RecordingUiFailureReporter _failures = new();

    public IndexPageTests()
    {
        Services.AddSingleton<SharedKernel.Blazor.IUiFailureReporter>(_failures);
        JSInterop.Mode = JSRuntimeMode.Loose;

        _fakeBookingsApi = new FakeBookingsApiClient();

        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<IToursApiClient>(new FakeToursApiClient());
        Services.AddSingleton<ICustomersApiClient>(new FakeCustomersApiClient());
    }

    [Fact]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Load_failure_shows_a_sanitized_error_instead_of_an_empty_state()
    {
        // Arrange
        _fakeBookingsApi.SetGetBookingPageException(new InvalidOperationException("database password leaked"));

        // Act
        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Find("[role='alert']"));

        // Assert
        var alert = cut.Find("[role='alert']");
        alert.TextContent.ShouldContain("We couldn't load bookings right now. Please try again.", StringComparison.Ordinal);
        alert.TextContent.ShouldNotContain("database password leaked", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Bookings Overview", StringComparison.Ordinal);
        _failures.Reports.ShouldBe([(nameof(InvalidOperationException), "bookings.index.load")]);
    }

    [Fact]
    public void Displays_total_bookings_count()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto());
        _fakeBookingsApi.AddBooking(BuildBookingDto());
        _fakeBookingsApi.AddBooking(BuildBookingDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Total: 3", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-secondary");
        badges.ShouldContain(b => b.TextContent.Contains("Total: 3", StringComparison.Ordinal));
    }

    [Fact]
    public void Displays_pending_bookings_count()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Pending: 2", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-warning");
        badges.ShouldContain(b => b.TextContent.Contains("Pending: 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Displays_confirmed_bookings_count()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Confirmed: 2", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-success");
        badges.ShouldContain(b => b.TextContent.Contains("Confirmed: 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Displays_empty_state_when_no_bookings()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Total: 0", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Pending: 0", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Confirmed: 0", StringComparison.Ordinal);
    }


    [Fact]
    public void Counts_only_pending_status_for_pending_badge()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Cancelled));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Completed));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Pending: 1", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Pending: 1", StringComparison.Ordinal);
    }

    [Fact]
    public void Counts_only_confirmed_status_for_confirmed_badge()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Cancelled));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Completed));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Confirmed: 1", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Confirmed: 1", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_all_status_counts_correctly()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Pending));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Confirmed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Cancelled));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Completed));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Total: 7", StringComparison.Ordinal), TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Total: 7", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Pending: 2", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Confirmed: 3", StringComparison.Ordinal);
    }

    [Fact]
    public void Displays_completed_and_cancelled_bookings_counts()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Completed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Completed));
        _fakeBookingsApi.AddBooking(BuildBookingDto(status: BookingStatusDto.Cancelled));

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        cut.Markup.ShouldContain("Completed: 2", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Cancelled: 1", StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_failure_while_loading_shows_a_safe_error_message()
    {
        // Arrange
        _fakeBookingsApi.SetGetBookingPageOutcome(FakeApiClientOutcomes.TransportFailure<GetBookingPageDto>());

        // Act
        var cut = Render<Index>();

        // Assert
        cut.WaitForAssertion(() => cut.Find("[role='alert']").TextContent.ShouldBe("We couldn't load bookings right now. Please try again."));
        _failures.Reports.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Booking Date", BookingPageSort.BookingDate)]
    [InlineData("Tour", BookingPageSort.TourName)]
    [InlineData("Customer", BookingPageSort.CustomerName)]
    [InlineData("Total Price", BookingPageSort.TotalPrice)]
    [Trait(TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public async Task Large_booking_overview_requests_pages_and_sorts_on_the_server(string title, BookingPageSort sort)
    {
        // Arrange
        var date = new DateTime(2029, 3, 4, 15, 0, 0, DateTimeKind.Utc);
        foreach (var index in Enumerable.Range(1, 800))
        {
            _fakeBookingsApi.AddBooking(BuildBookingDto(
                tourIdentifier: $"BOOKING-{index:D3}",
                bookingDate: date.AddDays(-index),
                totalPrice: index));
        }

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("BOOKING-010", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        var firstPage = cut.Markup;
        var paginator = cut.FindComponent<Paginator>();
        await cut.InvokeAsync(() => paginator.Instance.State.SetCurrentPageIndexAsync(1));
        cut.WaitForState(() => cut.Markup.Contains("BOOKING-020", StringComparison.Ordinal), TimeSpan.FromSeconds(2));
        ColumnBase<GetBookingDto> sortColumn = sort == BookingPageSort.BookingDate
            ? cut.FindComponent<PropertyColumn<GetBookingDto, DateTime>>().Instance
            : cut.FindComponents<TemplateColumn<GetBookingDto>>().ShouldHaveSingleItem(column => column.Instance.Title == title).Instance;
        var grid = cut.FindComponent<QuickGrid<GetBookingDto>>();
        await cut.InvokeAsync(() => grid.Instance.SortByColumnAsync(sortColumn, SortDirection.Descending));

        // Assert
        firstPage.ShouldContain("Total: 800", StringComparison.Ordinal);
        firstPage.ShouldNotContain("BOOKING-011", StringComparison.Ordinal);
        cut.Markup.ShouldContain("Total: 800", StringComparison.Ordinal);
        _fakeBookingsApi.GetAllBookingsCallCount.ShouldBe(0);
        _fakeBookingsApi.PageRequests[0].ShouldBe((1, BookingPageSort.Default, false));
        _fakeBookingsApi.PageRequests[1].ShouldBe((2, BookingPageSort.Default, false));
        var sortRequests = _fakeBookingsApi.PageRequests.Skip(2).ToArray();
        sortRequests.Length.ShouldBeInRange(1, 2);
        sortRequests.All(request => request.Sort == sort && request.Descending).ShouldBeTrue();
    }
}
