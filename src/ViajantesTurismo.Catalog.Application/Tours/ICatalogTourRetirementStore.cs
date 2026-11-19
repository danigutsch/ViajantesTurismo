using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>Persists public withdrawal independently of the lagging tour projection.</summary>
public interface ICatalogTourRetirementStore
{
    /// <summary>Atomically hides the retired tour and queues completion.</summary>
    ValueTask Withdraw(CatalogTourRetired retirement, CancellationToken ct);
}
