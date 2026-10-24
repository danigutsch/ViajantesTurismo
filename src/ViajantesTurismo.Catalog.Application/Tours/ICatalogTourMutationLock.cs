namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>
/// Serializes publication and gallery mutations for the same Catalog tours across application instances.
/// </summary>
public interface ICatalogTourMutationLock
{
    /// <summary>Acquires exclusive ownership of all supplied tours until the lease is disposed.</summary>
    /// <param name="catalogTourIds">The tours whose publication or gallery metadata will change.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A lease holding all locks in a consistent order.</returns>
    /// <exception cref="ArgumentNullException">The tour identifiers are null.</exception>
    ValueTask<IAsyncDisposable> Acquire(IReadOnlyCollection<Guid> catalogTourIds, CancellationToken ct);
}
