using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated save failure.");
}
