namespace ViajantesTurismo.Admin.WebTests.Components.Shared;

public sealed class ToastNotificationTests : BunitContext
{
    [Fact]
    public void Toast_Notification_Should_Not_Render_When_No_Toasts_Are_Shown()
    {
        // Act
        var cut = Render<ToastNotification>();

        // Assert
        var toastContainers = cut.FindAll(".toast-container");
        Assert.Empty(toastContainers);
    }

    [Fact]
    public void Toast_Notification_Should_Render_Toast_Container_When_Success_Toast_Is_Shown()
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
    public void Toast_Notification_Should_Display_Success_Message()
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
    public void Toast_Notification_Should_Display_Error_Message()
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
    public void Toast_Notification_Should_Apply_Success_Header_Class()
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
    public void Toast_Notification_Should_Apply_Error_Header_Class()
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
    public void Toast_Notification_Should_Display_Success_Icon()
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
    public void Toast_Notification_Should_Display_Error_Icon()
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
    public void Toast_Notification_Should_Display_Success_Title()
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
    public void Toast_Notification_Should_Display_Error_Title()
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
    public void Toast_Notification_Should_Render_Close_Button()
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
    public void Toast_Notification_Should_Remove_Toast_When_Close_Button_Is_Clicked()
    {
        // Arrange
        var cut = Render<ToastNotification>();
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Act
        var closeButton = cut.Find(".toast-header .btn-close");
        closeButton.Click();

        // Assert - wait for toast to be removed
        cut.WaitForState(() => cut.FindAll(".toast").Count == 0);
    }

    [Fact]
    public void Toast_Notification_Should_Display_Multiple_Toasts()
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
    public void Toast_Notification_Should_Display_Messages_In_Correct_Order()
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
    public void Toast_Notification_Should_Auto_Dismiss_After_Default_Duration()
    {
        // Arrange
        var cut = Render<ToastNotification>();
        cut.InvokeAsync(() => cut.Instance.ShowSuccess("Message"));

        // Assert - wait for toast to auto-dismiss (default 5000ms)
        cut.WaitForState(() => cut.FindAll(".toast").Count == 0, TimeSpan.FromSeconds(6));
    }
}
