namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>
/// Serializes mutations that can change a Tour's confirmed participant capacity.
/// </summary>
public interface ITourCapacityMutationLock
{
    /// <summary>
    /// Acquires the mutation lock for one Tour.
    /// </summary>
    /// <param name="tourId">The Tour identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A lease held until asynchronous disposal.</returns>
    ValueTask<IAsyncDisposable> Acquire(Guid tourId, CancellationToken ct);
}
