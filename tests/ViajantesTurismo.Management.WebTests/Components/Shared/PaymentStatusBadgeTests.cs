using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public sealed class PaymentStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(PaymentStatusDto.Unpaid, "bg-danger")]
    [InlineData(PaymentStatusDto.PartiallyPaid, "bg-warning")]
    [InlineData(PaymentStatusDto.Paid, "bg-success")]
    [InlineData(PaymentStatusDto.Refunded, "bg-info")]
    public void Payment_status_badge_should_apply_correct_css_class_for_each_status(
        PaymentStatusDto status,
        string expectedCssClass)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        (badge.ClassList).ShouldContain(expectedCssClass);
    }

    [Fact]
    public void Payment_status_badge_should_apply_text_dark_class_for_partially_paid_status()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.PartiallyPaid));

        // Assert
        var badge = cut.Find("span.badge");
        (badge.ClassList).ShouldContain("text-dark");
    }

    [Theory]
    [InlineData(PaymentStatusDto.Unpaid, "bi-currency-dollar")]
    [InlineData(PaymentStatusDto.PartiallyPaid, "bi-cash-stack")]
    [InlineData(PaymentStatusDto.Paid, "bi-check-circle-fill")]
    [InlineData(PaymentStatusDto.Refunded, "bi-arrow-counterclockwise")]
    public void Payment_status_badge_should_display_correct_icon_for_each_status(
        PaymentStatusDto status,
        string expectedIconClass)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var icon = cut.Find("span.badge i");
        (icon.ClassList).ShouldContain(expectedIconClass);
    }

    [Theory]
    [InlineData(PaymentStatusDto.Unpaid)]
    [InlineData(PaymentStatusDto.PartiallyPaid)]
    [InlineData(PaymentStatusDto.Paid)]
    [InlineData(PaymentStatusDto.Refunded)]
    public void Payment_status_badge_should_display_status_text(PaymentStatusDto status)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        (badge.TextContent).ShouldContain(EnumFormatter.Format(status), StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_status_badge_should_render_with_badge_base_class()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.Unpaid));

        // Assert
        var badge = cut.Find("span");
        (badge.ClassList).ShouldContain("badge");
    }

    [Fact]
    public void Payment_status_badge_should_render_bootstrap_icon_element()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.Paid));

        // Assert
        var icon = cut.Find("i");
        (icon.ClassList).ShouldContain("bi");
    }

    [Fact]
    public void Payment_status_badge_should_handle_all_enum_values_without_throwing()
    {
        // Arrange
        var allStatuses = Enum.GetValues<PaymentStatusDto>();

        // Act
        // Assert
        foreach (var status in allStatuses)
        {
            var cut = Render<PaymentStatusBadge>(parameters => parameters
                .Add(p => p.Status, status));

            var badge = cut.Find("span.badge");
            _ = (badge).ShouldNotBeNull();
        }
    }

    [Fact]
    public void Payment_status_badge_should_apply_default_styles_for_undefined_status_values()
    {
        // Arrange
        const PaymentStatusDto invalidStatus = (PaymentStatusDto)999;

        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, invalidStatus));

        // Assert
        var badge = cut.Find("span.badge");
        (badge.ClassList).ShouldContain("bg-secondary");

        var icon = cut.Find("i");
        (icon.ClassList).ShouldContain("bi-question-circle");
    }
}
