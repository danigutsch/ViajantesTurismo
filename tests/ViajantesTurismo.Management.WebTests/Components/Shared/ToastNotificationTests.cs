namespace ViajantesTurismo.Management.WebTests.Components.Shared;

public sealed class ToastNotificationTests : BunitContext
{
    [Fact]
    public void Toast_notification_should_not_render_when_no_toasts_are_shown()
    {
        // Act
        var cut = Render<ToastNotification>();

        // Assert
        var toastContainers = cut.FindAll(".toast-container");
        Assert.Empty(toastContainers);
    }

    [Fact]
    public void Toast_notification_should_render_toast_container_when_success_toast_is_shown()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Operation completed successfully"));

        // Assert
        var toastContainer = cut.Find(".toast-container");
        Assert.NotNull(toastContainer);
        Assert.Contains("position-fixed", toastContainer.ClassList);
        Assert.Contains("top-0", toastContainer.ClassList);
        Assert.Contains("end-0", toastContainer.ClassList);
    }

    [Fact]
    public void Toast_notification_should_display_success_message()
    {
        // Arrange
        const string successMessage = "Customer created successfully!";
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess(successMessage));

        // Assert
        var toastBody = cut.Find(".toast-body");
        Assert.Equal(successMessage, toastBody.TextContent.Trim());
    }

    [Fact]
    public void Toast_notification_should_display_error_message()
    {
        // Arrange
        const string errorMessage = "Failed to save customer";
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowError(errorMessage));

        // Assert
        var toastBody = cut.Find(".toast-body");
        Assert.Equal(errorMessage, toastBody.TextContent.Trim());
    }

    [Fact]
    public void Toast_notification_should_apply_success_header_class()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Success!"));

        // Assert
        var toastHeader = cut.Find(".toast-header");
        Assert.Contains("bg-success", toastHeader.ClassList);
        Assert.Contains("text-white", toastHeader.ClassList);
    }

    [Fact]
    public void Toast_notification_should_apply_error_header_class()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowError("Error!"));

        // Assert
        var toastHeader = cut.Find(".toast-header");
        Assert.Contains("bg-danger", toastHeader.ClassList);
        Assert.Contains("text-white", toastHeader.ClassList);
    }

    [Fact]
    public void Toast_notification_should_display_success_icon()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Success!"));

        // Assert
        var icon = cut.Find(".toast-header i");
        Assert.Contains("bi-check-circle-fill", icon.ClassList);
    }

    [Fact]
    public void Toast_notification_should_display_error_icon()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowError("Error!"));

        // Assert
        var icon = cut.Find(".toast-header i");
        Assert.Contains("bi-exclamation-triangle-fill", icon.ClassList);
    }

    [Fact]
    public void Toast_notification_should_display_success_title()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Assert
        var title = cut.Find(".toast-header strong");
        Assert.Equal("Success", title.TextContent);
    }

    [Fact]
    public void Toast_notification_should_display_error_title()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowError("Message"));

        // Assert
        var title = cut.Find(".toast-header strong");
        Assert.Equal("Error", title.TextContent);
    }

    [Fact]
    public void Toast_notification_should_render_close_button()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Assert
        var closeButton = cut.Find(".toast-header .btn-close");
        Assert.NotNull(closeButton);
    }

    [Fact]
    public void Toast_notification_should_remove_toast_when_close_button_is_clicked()
    {
        // Arrange
        var cut = Render<ToastNotification>();
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Act
        var closeButton = cut.Find(".toast-header .btn-close");
        closeButton.Click();

        // Assert
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast")));
    }

    [Fact]
    public void Toast_notification_should_display_multiple_toasts()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() =>
        {
            cut.Instance.ShowSuccess("First message");
            cut.Instance.ShowError("Second message");
        });

        // Assert
        var toasts = cut.FindAll(".toast");
        Assert.Equal(2, toasts.Count);
    }

    [Fact]
    public void Toast_notification_should_display_messages_in_correct_order()
    {
        // Arrange
        var cut = Render<ToastNotification>();

        // Act
        cut.InvokeAsync(() =>
        {
            cut.Instance.ShowSuccess("First");
            cut.Instance.ShowError("Second");
        });

        // Assert
        var toastBodies = cut.FindAll(".toast-body");
        Assert.Equal("First", toastBodies[0].TextContent.Trim());
        Assert.Equal("Second", toastBodies[1].TextContent.Trim());
    }

    [Fact]
    public void Toast_notification_should_auto_dismiss_after_default_duration()
    {
        // Arrange
        var cut = Render<ToastNotification>();
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Assert
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".toast")), TimeSpan.FromSeconds(6));
    }
}
