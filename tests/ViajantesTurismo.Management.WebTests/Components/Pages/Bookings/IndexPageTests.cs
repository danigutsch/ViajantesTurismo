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
    [Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.ComponentScope)]
    [Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.ComponentCategory)]
    public void Load_failure_shows_a_sanitized_error_instead_of_an_empty_state()
    {
        // Arrange
        _fakeBookingsApi.SetGetAllBookingsException(new InvalidOperationException("database password leaked"));

        // Act
        var cut = Render<Index>();
        cut.WaitForAssertion(() => cut.Find("[role='alert']"));

        // Assert
        var alert = cut.Find("[role='alert']");
        alert.TextContent.ShouldContain("We couldn't load bookings right now. Please try again.", StringComparison.Ordinal);
        alert.TextContent.ShouldNotContain("database password leaked", StringComparison.Ordinal);
        cut.Markup.ShouldNotContain("Bookings Overview", StringComparison.Ordinal);
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
        (badges).ShouldContain(b => b.TextContent.Contains("Total: 3", StringComparison.Ordinal));
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
        (badges).ShouldContain(b => b.TextContent.Contains("Pending: 2", StringComparison.Ordinal));
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
        (badges).ShouldContain(b => b.TextContent.Contains("Confirmed: 2", StringComparison.Ordinal));
    }

    [Fact]
    public void Displays_empty_state_when_no_bookings()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        (cut.Markup).ShouldContain("Total: 0", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Pending: 0", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Confirmed: 0", StringComparison.Ordinal);
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
        (cut.Markup).ShouldContain("Pending: 1", StringComparison.Ordinal);
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
        (cut.Markup).ShouldContain("Confirmed: 1", StringComparison.Ordinal);
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
        (cut.Markup).ShouldContain("Total: 7", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Pending: 2", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Confirmed: 3", StringComparison.Ordinal);
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
        (cut.Markup).ShouldContain("Completed: 2", StringComparison.Ordinal);
        (cut.Markup).ShouldContain("Cancelled: 1", StringComparison.Ordinal);
    }
}
