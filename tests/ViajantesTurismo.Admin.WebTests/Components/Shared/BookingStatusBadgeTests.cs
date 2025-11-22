using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Components.Shared;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

/// <summary>
/// Tests for the BookingStatusBadge component.
/// </summary>
public sealed class BookingStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(BookingStatusDto.Pending, "bg-warning")]
    [InlineData(BookingStatusDto.Confirmed, "bg-success")]
    [InlineData(BookingStatusDto.Cancelled, "bg-danger")]
    [InlineData(BookingStatusDto.Completed, "bg-primary")]
    public void Booking_Status_Badge_Should_Apply_Correct_Css_Class_For_Each_Status(
        BookingStatusDto status,
        string expectedCssClass)
    {
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(expectedCssClass, badge.ClassList);
    }

    [Fact]
    public void Booking_Status_Badge_Should_Apply_Text_Dark_Class_For_Pending_Status()
    {
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, BookingStatusDto.Pending));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("text-dark", badge.ClassList);
    }

    [Theory]
    [InlineData(BookingStatusDto.Pending, "bi-clock")]
    [InlineData(BookingStatusDto.Confirmed, "bi-check-circle")]
    [InlineData(BookingStatusDto.Cancelled, "bi-x-circle")]
    [InlineData(BookingStatusDto.Completed, "bi-check-all")]
    public void Booking_Status_Badge_Should_Display_Correct_Icon_For_Each_Status(
        BookingStatusDto status,
        string expectedIconClass)
    {
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var icon = cut.Find("span.badge i");
        Assert.Contains(expectedIconClass, icon.ClassList);
    }

    [Theory]
    [InlineData(BookingStatusDto.Pending)]
    [InlineData(BookingStatusDto.Confirmed)]
    [InlineData(BookingStatusDto.Cancelled)]
    [InlineData(BookingStatusDto.Completed)]
    public void Booking_Status_Badge_Should_Display_Status_Text(BookingStatusDto status)
    {
        // Arrange
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(status.ToString(), badge.TextContent);
    }

    [Fact]
    public void Booking_Status_Badge_Should_Render_With_Badge_Base_Class()
    {
        // Arrange
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, BookingStatusDto.Pending));

        // Assert
        var badge = cut.Find("span");
        Assert.Contains("badge", badge.ClassList);
    }

    [Fact]
    public void Booking_Status_Badge_Should_Render_Bootstrap_Icon_Element()
    {
        // Arrange
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, BookingStatusDto.Confirmed));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("bi", icon.ClassList);
    }

    [Fact]
    public void Booking_Status_Badge_Should_Handle_All_Enum_Values_Without_Throwing()
    {
        // Arrange
        var allStatuses = Enum.GetValues<BookingStatusDto>();

        // Act
        // Assert
        foreach (var status in allStatuses)
        {
            var cut = Render<BookingStatusBadge>(parameters => parameters
                .Add(p => p.Status, status));

            var badge = cut.Find("span.badge");
            Assert.NotNull(badge);
        }
    }

    [Fact]
    public void Booking_Status_Badge_Should_Apply_Default_Styles_For_Undefined_Status_Values()
    {
        // Arrange
        const BookingStatusDto invalidStatus = (BookingStatusDto)999;

        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, invalidStatus));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("bg-secondary", badge.ClassList);

        var icon = cut.Find("i");
        Assert.Contains("bi-question-circle", icon.ClassList);
    }
}
