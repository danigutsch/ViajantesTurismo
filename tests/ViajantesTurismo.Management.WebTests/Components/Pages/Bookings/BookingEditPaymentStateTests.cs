using ViajantesTurismo.Management.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class BookingEditPaymentStateTests
{
    [Fact]
    public void ShowForm_resets_model_and_makes_form_visible()
    {
        // Arrange
        var state = new BookingEditPaymentState();
        state.ShowForm();
        state.FormModel.Amount = 123.45m;
        state.HideForm();

        // Act
        state.ShowForm();

        // Assert
        Assert.True(state.IsFormVisible);
        Assert.Null(state.FormModel.Amount);
    }

    [Fact]
    public void HideForm_makes_form_not_visible()
    {
        // Arrange
        var state = new BookingEditPaymentState();
        state.ShowForm();

        // Act
        state.HideForm();

        // Assert
        Assert.False(state.IsFormVisible);
    }

    [Fact]
    public void BeginAndEndSubmission_toggle_submitting_state()
    {
        // Arrange
        var state = new BookingEditPaymentState();

        // Act
        state.BeginSubmission();

        // Assert
        Assert.True(state.IsSubmitting);

        // Act
        state.EndSubmission();

        // Assert
        Assert.False(state.IsSubmitting);
    }
}
