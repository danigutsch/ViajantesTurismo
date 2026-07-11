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
        (badge.ClassList).ShouldContain(expectedCssClass);
    }

    [Fact]
    public void Booking_status_badge_should_apply_text_dark_class_for_pending_status()
    {
        // Act
        var cut = Render<BookingStatusBadge>(parameters => parameters
            .Add(p => p.Status, BookingStatusDto.Pending));

        // Assert
        var badge = cut.Find("span.badge");
        (badge.ClassList).ShouldContain("text-dark");
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
        (icon.ClassList).ShouldContain(expectedIconClass);
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
        (badge.TextContent).ShouldContain(status.ToString(), StringComparison.Ordinal);
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
        (badge.ClassList).ShouldContain("badge");
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
        (icon.ClassList).ShouldContain("bi");
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
            _ = (badge).ShouldNotBeNull();
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
        (badge.ClassList).ShouldContain("bg-secondary");

        var icon = cut.Find("i");
        (icon.ClassList).ShouldContain("bi-question-circle");
    }
}
