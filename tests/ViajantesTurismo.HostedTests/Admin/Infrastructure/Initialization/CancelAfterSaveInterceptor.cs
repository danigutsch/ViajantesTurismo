using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Initialization;

/// <summary>Cancels the supplied source after a save completes and throws <see cref="OperationCanceledException" />.</summary>
/// <param name="cancellation">Source canceled once changes have been saved.</param>
internal sealed class CancelAfterSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct)
    {
        await cancellation.CancelAsync();
        throw new OperationCanceledException(cancellation.Token);
    }
}
