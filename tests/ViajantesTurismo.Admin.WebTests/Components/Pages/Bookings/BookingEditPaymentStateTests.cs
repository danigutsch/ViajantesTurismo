using ViajantesTurismo.Admin.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Bookings;

public sealed class BookingEditPaymentStateTests
{
    [Fact]
    public void ShowForm_Resets_Model_And_Makes_Form_Visible()
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
    public void HideForm_Makes_Form_Not_Visible()
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
    public void BeginAndEndSubmission_Toggle_Submitting_State()
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
