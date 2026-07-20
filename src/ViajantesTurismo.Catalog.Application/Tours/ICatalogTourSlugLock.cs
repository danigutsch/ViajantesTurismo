namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Serializes Catalog tour slug claims across application instances.
/// </summary>
public interface ICatalogTourSlugLock
{
    /// <summary>
    /// Acquires exclusive ownership of the normalized slug until the returned lease is disposed.
    /// </summary>
    /// <param name="slug">The normalized public slug.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An asynchronous lock lease.</returns>
    ValueTask<IAsyncDisposable> Acquire(string slug, CancellationToken ct);
}
