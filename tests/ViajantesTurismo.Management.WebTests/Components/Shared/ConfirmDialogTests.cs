namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public sealed class ConfirmDialogTests : BunitContext
{
    [Fact]
    public void Confirm_dialog_should_not_render_when_not_visible()
    {
        // Act
        var cut = Render<ConfirmDialog>();

        // Assert
        var modals = cut.FindAll(".modal");
        (modals).ShouldBeEmpty();
    }

    [Fact]
    public void Confirm_dialog_should_render_modal_when_showasync_is_called()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Are you sure you want to delete this item?"));

        // Assert
        var modal = cut.Find(".modal");
        _ = (modal).ShouldNotBeNull();
        (modal.ClassList).ShouldContain("show");
    }

    [Fact]
    public void Confirm_dialog_should_display_custom_message()
    {
        // Arrange
        const string customMessage = "Do you want to proceed with this action?";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync(customMessage));

        // Assert
        var messageElement = cut.Find(".modal-body p");
        (messageElement.TextContent).ShouldBe(customMessage);
    }

    [Fact]
    public void Confirm_dialog_should_display_custom_title()
    {
        // Arrange
        const string customTitle = "Delete Confirmation";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", title: customTitle));

        // Assert
        var titleElement = cut.Find(".modal-title");
        (titleElement.TextContent).ShouldBe(customTitle);
    }

    [Fact]
    public void Confirm_dialog_should_display_default_title_when_not_specified()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var titleElement = cut.Find(".modal-title");
        (titleElement.TextContent).ShouldBe("Confirm");
    }

    [Fact]
    public void Confirm_dialog_should_display_custom_confirm_button_text()
    {
        // Arrange
        const string customConfirmText = "Yes, Delete";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", confirmText: customConfirmText));

        // Assert
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        (confirmButton.TextContent).ShouldContain(customConfirmText, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_dialog_should_display_custom_cancel_button_text()
    {
        // Arrange
        const string customCancelText = "No, Keep It";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", cancelText: customCancelText));

        // Assert
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        (cancelButton.TextContent).ShouldContain(customCancelText, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirm_dialog_should_apply_custom_confirm_button_class()
    {
        // Arrange
        const string customButtonClass = "btn-danger";
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message", confirmButtonClass: customButtonClass));

        // Assert
        var confirmButton = cut.Find($".modal-footer .{customButtonClass}");
        _ = (confirmButton).ShouldNotBeNull();
    }

    [Fact]
    public void Confirm_dialog_should_apply_default_primary_button_class()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        _ = (confirmButton).ShouldNotBeNull();
    }

    [Fact]
    public void Confirm_dialog_should_render_close_button_in_header()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var closeButton = cut.Find(".modal-header .btn-close");
        _ = (closeButton).ShouldNotBeNull();
    }

    [Fact]
    public void Confirm_dialog_should_render_modal_backdrop()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));

        // Assert
        var backdrop = cut.Find(".modal-backdrop");
        _ = (backdrop).ShouldNotBeNull();
        (backdrop.ClassList).ShouldContain("show");
    }

    [Fact]
    public async Task Confirm_dialog_should_return_true_when_confirm_button_is_clicked()
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
        (result).ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_dialog_should_return_false_when_cancel_button_is_clicked()
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
        (result).ShouldBeFalse();
    }

    [Fact]
    public async Task Confirm_dialog_should_return_false_when_close_button_is_clicked()
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
        (result).ShouldBeFalse();
    }

    [Fact]
    public void Confirm_dialog_should_hide_modal_after_confirm_is_clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

        // Act
        var confirmButton = cut.Find(".modal-footer .btn-primary");
        confirmButton.Click();

        // Assert
        cut.WaitForAssertion(() => (cut.FindAll(".modal")).ShouldBeEmpty());
    }

    [Fact]
    public void Confirm_dialog_should_hide_modal_after_cancel_is_clicked()
    {
        // Arrange
        var cut = Render<ConfirmDialog>();
        cut.InvokeAsync(() => cut.Instance.ShowAsync("Message"));
        cut.WaitForState(() => cut.FindAll(".modal").Count > 0);

        // Act
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        cancelButton.Click();

        // Assert
        cut.WaitForAssertion(() => (cut.FindAll(".modal")).ShouldBeEmpty());
    }
}
