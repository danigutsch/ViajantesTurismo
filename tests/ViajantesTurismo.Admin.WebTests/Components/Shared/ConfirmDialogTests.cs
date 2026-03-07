using ViajantesTurismo.Admin.Web.Components.Shared;

namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public sealed class ConfirmDialogTests : BunitContext
{
    [Fact]
    public void Confirm_Dialog_Should_Not_Render_When_Not_Visible()
    {
        // Act
        var cut = Render<ConfirmDialog>();

        // Assert
        var modals = cut.FindAll(".modal");
        Assert.Empty(modals);
    }

    [Fact]
    public void Confirm_Dialog_Should_Render_Modal_When_ShowAsync_Is_Called()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Are you sure you want to delete this item?"));

        // Assert
        var modal = cut.Find(".modal");
        Assert.NotNull(modal);
        Assert.Contains("show", modal.ClassList);
    }

    [Fact]
    public void Confirm_Dialog_Should_Display_Custom_Message()
    {
        // Arrange
        const string customMessage = "Do you want to proceed with this action?";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync(customMessage));

        // Assert
        var messageElement = cut.Find(".modal-body p");
        Assert.Equal(customMessage, messageElement.TextContent);
    }

    [Fact]
    public void Confirm_Dialog_Should_Display_Custom_Title()
    {
        // Arrange
        const string customTitle = "Delete Confirmation";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", title: customTitle));

        // Assert
        var titleElement = cut.Find(".modal-title");
        Assert.Equal(customTitle, titleElement.TextContent);
    }

    [Fact]
    public void Confirm_Dialog_Should_Display_Default_Title_When_Not_Specified()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var titleElement = cut.Find(".modal-title");
        Assert.Equal("Confirm", titleElement.TextContent);
    }

    [Fact]
    public void Confirm_Dialog_Should_Display_Custom_Confirm_Button_Text()
    {
        // Arrange
        const string customConfirmText = "Yes, Delete";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", confirmText: customConfirmText));

        // Assert
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        Assert.Contains(customConfirmText, confirmButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_Dialog_Should_Display_Custom_Cancel_Button_Text()
    {
        // Arrange
        const string customCancelText = "No, Keep It";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", cancelText: customCancelText));

        // Assert
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        Assert.Contains(customCancelText, cancelButton.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_Dialog_Should_Apply_Custom_Confirm_Button_Class()
    {
        // Arrange
        const string customButtonClass = "btn-danger";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", confirmButtonClass: customButtonClass));

        // Assert
        var confirmButton = cut.Find($".modal-footer .{customButtonClass}");
        Assert.NotNull(confirmButton);
    }

    [Fact]
    public void Confirm_Dialog_Should_Apply_Default_Primary_Button_Class()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        Assert.NotNull(confirmButton);
    }

    [Fact]
    public void Confirm_Dialog_Should_Render_Close_Button_In_Header()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var closeButton = cut.Find(".modal-header .btn-close");
        Assert.NotNull(closeButton);
    }

    [Fact]
    public void Confirm_Dialog_Should_Render_Modal_Backdrop()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var backdrop = cut.Find(".modal-backdrop");
        Assert.NotNull(backdrop);
        Assert.Contains("show", backdrop.ClassList);
    }

    [Fact]
    public async Task Confirm_Dialog_Should_Return_True_When_Confirm_Button_Is_Clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        await cut.WaitForStateAsync(() => cut.FindAll(".modal").Count > 0);

        // Act
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        await confirmButton.ClickAsync();

        // Assert
        var result = await resultTask;
        Assert.True(result);
    }

    [Fact]
    public async Task Confirm_Dialog_Should_Return_False_When_Cancel_Button_Is_Clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        await cut.WaitForStateAsync(() => cut.FindAll(".modal").Count > 0);

        // Act
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        await cancelButton.ClickAsync();

        // Assert
        var result = await resultTask;
        Assert.False(result);
    }

    [Fact]
    public async Task Confirm_Dialog_Should_Return_False_When_Close_Button_Is_Clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        var resultTask = cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        await cut.WaitForStateAsync(() => cut.FindAll(".modal").Count > 0);

        // Act
        var closeButton = cut.Find(".modal-header .btn-close");
        await closeButton.ClickAsync();

        // Assert
        var result = await resultTask;
        Assert.False(result);
    }

    [Fact]
    public void Confirm_Dialog_Should_Hide_Modal_After_Confirm_Is_Clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

        // Act
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        confirmButton.Click();

        // Assert - wait for modal to be removed from DOM
        cut.WaitForState(() => cut.FindAll(".modal").Count == 0);
    }

    [Fact]
    public void Confirm_Dialog_Should_Hide_Modal_After_Cancel_Is_Clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

        // Act
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        cancelButton.Click();

        // Assert - wait for modal to be removed from DOM
        cut.WaitForState(() => cut.FindAll(".modal").Count == 0);
    }
}
