using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Throws a simulated failure on the first save and lets later saves pass through.
/// </summary>
internal sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
{
    private bool _hasFailed;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct)
    {
        if (_hasFailed)
        {
            return ValueTask.FromResult(result);
        }

        _hasFailed = true;

        throw new InvalidOperationException("Simulated save failure.");
    }
}
