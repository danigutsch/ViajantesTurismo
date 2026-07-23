using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

internal sealed class CancelAfterSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await cancellation.CancelAsync();
        throw new OperationCanceledException(cancellation.Token);
    }
}
