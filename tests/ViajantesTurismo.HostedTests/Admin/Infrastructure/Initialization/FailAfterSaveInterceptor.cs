using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Initialization;

/// <summary>Throws <see cref="InvalidOperationException" /> after every save, before the initialization commits.</summary>
internal sealed class FailAfterSaveInterceptor : SaveChangesInterceptor
{
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct)
    {
        throw new InvalidOperationException("Simulated failure before initialization commit.");
    }
}
