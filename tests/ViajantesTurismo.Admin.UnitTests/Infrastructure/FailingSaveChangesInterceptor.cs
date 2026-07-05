using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
{
    private bool _hasFailed;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_hasFailed)
        {
            return ValueTask.FromResult(result);
        }

        _hasFailed = true;

        throw new InvalidOperationException("Simulated save failure.");
    }
}
