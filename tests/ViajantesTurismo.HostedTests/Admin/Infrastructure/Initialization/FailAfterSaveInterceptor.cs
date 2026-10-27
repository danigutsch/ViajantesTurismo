using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

/// <summary>Throws <see cref="InvalidOperationException" /> after every save, before the initialization commits.</summary>
internal sealed class FailAfterSaveInterceptor : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated failure before initialization commit.");
    }
}
