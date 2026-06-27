namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public sealed class BookingStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(BookingStatusDto.Pending, "bg-warning")]
    [InlineData(BookingStatusDto.Confirmed, "bg-success")]
    [InlineData(BookingStatusDto.Cancelled, "bg-danger")]
    [InlineData(BookingStatusDto.Completed, "bg-primary")]
    public void Booking_status_badge_should_apply_correct_css_class_for_each_status(
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
    public void Booking_status_badge_should_apply_text_dark_class_for_pending_status()
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
    public void Booking_status_badge_should_display_correct_icon_for_each_status(
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
    public void Booking_status_badge_should_display_status_text(BookingStatusDto status)
    {
        // Arrange
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(status.ToString(), badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Booking_status_badge_should_render_with_badge_base_class()
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
    public void Booking_status_badge_should_render_bootstrap_icon_element()
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
    public void Booking_status_badge_should_handle_all_enum_values_without_throwing()
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
    public void Booking_status_badge_should_apply_default_styles_for_undefined_status_values()
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
