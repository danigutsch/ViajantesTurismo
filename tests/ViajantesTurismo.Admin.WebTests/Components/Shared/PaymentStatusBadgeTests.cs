using ViajantesTurismo.Admin.Web.Helpers;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public sealed class PaymentStatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData(PaymentStatusDto.Unpaid, "bg-danger")]
    [InlineData(PaymentStatusDto.PartiallyPaid, "bg-warning")]
    [InlineData(PaymentStatusDto.Paid, "bg-success")]
    [InlineData(PaymentStatusDto.Refunded, "bg-info")]
    public void Payment_Status_Badge_Should_Apply_Correct_Css_Class_For_Each_Status(
        PaymentStatusDto status,
        string expectedCssClass)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(expectedCssClass, badge.ClassList);
    }

    [Fact]
    public void Payment_Status_Badge_Should_Apply_Text_Dark_Class_For_Partially_Paid_Status()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.PartiallyPaid));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("text-dark", badge.ClassList);
    }

    [Theory]
    [InlineData(PaymentStatusDto.Unpaid, "bi-currency-dollar")]
    [InlineData(PaymentStatusDto.PartiallyPaid, "bi-cash-stack")]
    [InlineData(PaymentStatusDto.Paid, "bi-check-circle-fill")]
    [InlineData(PaymentStatusDto.Refunded, "bi-arrow-counterclockwise")]
    public void Payment_Status_Badge_Should_Display_Correct_Icon_For_Each_Status(
        PaymentStatusDto status,
        string expectedIconClass)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var icon = cut.Find("span.badge i");
        Assert.Contains(expectedIconClass, icon.ClassList);
    }

    [Theory]
    [InlineData(PaymentStatusDto.Unpaid)]
    [InlineData(PaymentStatusDto.PartiallyPaid)]
    [InlineData(PaymentStatusDto.Paid)]
    [InlineData(PaymentStatusDto.Refunded)]
    public void Payment_Status_Badge_Should_Display_Status_Text(PaymentStatusDto status)
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains(EnumFormatter.Format(status), badge.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_Status_Badge_Should_Render_With_Badge_Base_Class()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.Unpaid));

        // Assert
        var badge = cut.Find("span");
        Assert.Contains("badge", badge.ClassList);
    }

    [Fact]
    public void Payment_Status_Badge_Should_Render_Bootstrap_Icon_Element()
    {
        // Arrange
        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, PaymentStatusDto.Paid));

        // Assert
        var icon = cut.Find("i");
        Assert.Contains("bi", icon.ClassList);
    }

    [Fact]
    public void Payment_Status_Badge_Should_Handle_All_Enum_Values_Without_Throwing()
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
            Assert.NotNull(badge);
        }
    }

    [Fact]
    public void Payment_Status_Badge_Should_Apply_Default_Styles_For_Undefined_Status_Values()
    {
        // Arrange
        const PaymentStatusDto invalidStatus = (PaymentStatusDto)999;

        // Act
        var cut = Render<PaymentStatusBadge>(parameters => parameters
            .Add(p => p.Status, invalidStatus));

        // Assert
        var badge = cut.Find("span.badge");
        Assert.Contains("bg-secondary", badge.ClassList);

        var icon = cut.Find("i");
        Assert.Contains("bi-question-circle", icon.ClassList);
    }
}
