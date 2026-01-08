using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Tests.Shared;
using ViajantesTurismo.Admin.Web.Components.Shared;
using static ViajantesTurismo.Admin.Tests.Shared.DtoBuilders;
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
    public void Renders_Page_Title()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("h1").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var pageTitle = cut.Find("h1");
        Assert.Contains("All Bookings", pageTitle.TextContent);
    }

    [Fact]
    public void Renders_Loading_State_Initially()
    {
        // Arrange
        // Act
        var cut = Render<Index>();

        // Assert - Component should show loading then load data
        cut.WaitForState(() => !cut.Markup.Contains("Loading bookings..."), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Renders_Bookings_Overview_Card_Header()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var cardHeader = cut.Find(".card-header");
        Assert.Contains("Bookings Overview", cardHeader.TextContent);
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
        cut.WaitForState(() => cut.Markup.Contains("Total: 3"), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-secondary");
        Assert.Contains(badges, b => b.TextContent.Contains("Total: 3"));
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
        cut.WaitForState(() => cut.Markup.Contains("Pending: 2"), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-warning");
        Assert.Contains(badges, b => b.TextContent.Contains("Pending: 2"));
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
        cut.WaitForState(() => cut.Markup.Contains("Confirmed: 2"), TimeSpan.FromSeconds(2));

        // Assert
        var badges = cut.FindAll("span.badge.bg-success");
        Assert.Contains(badges, b => b.TextContent.Contains("Confirmed: 2"));
    }

    [Fact]
    public void Renders_BookingsList_Component()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll("table").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var table = cut.Find("table");
        Assert.NotNull(table);
    }

    [Fact]
    public void Displays_Empty_State_When_No_Bookings()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-header").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total: 0", cut.Markup);
        Assert.Contains("Pending: 0", cut.Markup);
        Assert.Contains("Confirmed: 0", cut.Markup);
    }

    [Fact]
    public void Renders_Multiple_Bookings_In_List()
    {
        // Arrange
        var booking1 = BuildBookingDto(status: BookingStatusDto.Pending);
        var booking2 = BuildBookingDto(status: BookingStatusDto.Confirmed);
        var booking3 = BuildBookingDto(status: BookingStatusDto.Completed);

        _fakeBookingsApi.AddBooking(booking1);
        _fakeBookingsApi.AddBooking(booking2);
        _fakeBookingsApi.AddBooking(booking3);

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.Markup.Contains("Total: 3"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total: 3", cut.Markup);
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
        cut.WaitForState(() => cut.Markup.Contains("Pending: 1"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Pending: 1", cut.Markup);
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
        cut.WaitForState(() => cut.Markup.Contains("Confirmed: 1"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Confirmed: 1", cut.Markup);
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
        cut.WaitForState(() => cut.Markup.Contains("Total: 7"), TimeSpan.FromSeconds(2));

        // Assert
        Assert.Contains("Total: 7", cut.Markup);
        Assert.Contains("Pending: 2", cut.Markup);
        Assert.Contains("Confirmed: 3", cut.Markup);
    }

    [Fact]
    public void Renders_Card_Body_With_BookingsList()
    {
        // Arrange
        _fakeBookingsApi.AddBooking(BuildBookingDto());

        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".card-body").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var cardBody = cut.Find(".card-body");
        Assert.NotNull(cardBody);
    }

    [Fact]
    public void Uses_Container_Fluid_Layout()
    {
        // Arrange
        // Act
        var cut = Render<Index>();
        cut.WaitForState(() => cut.FindAll(".container-fluid").Count > 0, TimeSpan.FromSeconds(2));

        // Assert
        var container = cut.Find(".container-fluid");
        Assert.NotNull(container);
    }
}
