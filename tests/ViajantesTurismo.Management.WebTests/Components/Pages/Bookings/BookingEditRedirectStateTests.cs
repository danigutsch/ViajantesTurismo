using ViajantesTurismo.Management.Web.Components.Pages.Bookings;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Bookings;

public sealed class BookingEditRedirectStateTests
{
    [Fact]
    public async Task Reset_clears_pending_and_cancelled_state()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();
        state.BeginPendingRedirect();
        await state.CancelPendingRedirect();

        // Act
        await state.Reset();

        // Assert
        TestAssert.False(state.IsPending);
        TestAssert.False(state.IsCancelled);
    }

    [Fact]
    public async Task BeginPendingRedirect_marks_state_as_pending_and_navigable()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();

        // Act
        var token = state.BeginPendingRedirect();

        // Assert
        TestAssert.True(state.IsPending);
        TestAssert.False(state.IsCancelled);
        TestAssert.True(state.CanNavigate(token));
    }

    [Fact]
    public async Task CancelPendingRedirect_marks_state_as_cancelled_and_not_pending()
    {
        // Arrange
        await using var state = new BookingEditRedirectState();
        var token = state.BeginPendingRedirect();

        // Act
        await state.CancelPendingRedirect();

        // Assert
        TestAssert.False(state.IsPending);
        TestAssert.True(state.IsCancelled);
        TestAssert.False(state.CanNavigate(token));
    }
}
