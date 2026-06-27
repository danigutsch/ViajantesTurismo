using Index = ViajantesTurismo.Management.Web.Components.Pages.Bookings.Index;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public class IndexPageTests : BunitContext
{
    private readonly FakeBookingsApiClient _fakeBookingsApi;

    public IndexPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _fakeBookingsApi = new FakeBookingsApiClient();

        Services.AddSingleton<IBookingsApiClient>(_fakeBookingsApi);
        Services.AddSingleton<IToursApiClient>(new FakeToursApiClient());
        Services.AddSingleton<ICustomersApiClient>(new FakeCustomersApiClient());
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
        Assert.Contains(badges, b => b.TextContent.Contains("Total: 3", StringComparison.Ordinal));
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
        Assert.Contains(badges, b => b.TextContent.Contains("Pending: 2", StringComparison.Ordinal));
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
        Assert.Contains(badges, b => b.TextContent.Contains("Confirmed: 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Displays_empty_state_when_no_bookings()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total: 0", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pending: 0", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Confirmed: 0", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("Pending: 1", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("Confirmed: 1", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("Total: 7", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Pending: 2", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Confirmed: 3", cut.Markup, StringComparison.Ordinal);
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
        Assert.Contains("Completed: 2", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Cancelled: 1", cut.Markup, StringComparison.Ordinal);
    }
}
