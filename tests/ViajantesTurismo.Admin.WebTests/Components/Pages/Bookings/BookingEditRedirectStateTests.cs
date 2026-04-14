using ViajantesTurismo.Admin.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Bookings;

public sealed class BookingEditRedirectStateTests
{
    [Fact]
    public async Task Reset_Clears_Pending_And_Cancelled_State()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();
        state.BeginPendingRedirect();
        await state.CancelPendingRedirect();

        // Act
        await state.Reset();

        // Assert
        Assert.False(state.IsPending);
        Assert.False(state.IsCancelled);
    }

    [Fact]
    public async Task BeginPendingRedirect_Marks_State_As_Pending_And_Navigable()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();

        // Act
        var token = state.BeginPendingRedirect();

        // Assert
        Assert.True(state.IsPending);
        Assert.False(state.IsCancelled);
        Assert.True(state.CanNavigate(token));
    }

    [Fact]
    public async Task CancelPendingRedirect_Marks_State_As_Cancelled_And_Not_Pending()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();
        var token = state.BeginPendingRedirect();

        // Act
        await state.CancelPendingRedirect();

        // Assert
        Assert.False(state.IsPending);
        Assert.True(state.IsCancelled);
        Assert.False(state.CanNavigate(token));
    }
}
