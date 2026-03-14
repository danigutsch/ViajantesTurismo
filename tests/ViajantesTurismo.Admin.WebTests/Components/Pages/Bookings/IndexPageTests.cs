using Index = ViajantesTurismo.Admin.Web.Components.Pages.Bookings.Index;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Bookings;

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
    public void Displays_Total_Bookings_Count()
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
    public void Displays_Pending_Bookings_Count()
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
    public void Displays_Confirmed_Bookings_Count()
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
    public void Displays_Empty_State_When_No_Bookings()
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
    public void Counts_Only_Pending_Status_For_Pending_Badge()
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
    public void Counts_Only_Confirmed_Status_For_Confirmed_Badge()
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
    public void Displays_All_Status_Counts_Correctly()
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
}
