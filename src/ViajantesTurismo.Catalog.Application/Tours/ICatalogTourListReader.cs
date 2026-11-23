using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Application.Tours;

/// <summary>Reads summary pages for the Management tour list.</summary>
public interface ICatalogTourListReader
{
    /// <summary>Returns a page ordered by the given field and a stable tour identifier tie-breaker.</summary>
    /// <param name="page">The requested page text, or <see langword="null"/> for the first page.</param>
    /// <param name="sort">The selected sort field.</param>
    /// <param name="descending">Whether the selected sort field is descending.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The bounded summaries and total count.</returns>
    ValueTask<CatalogTourPageDto> ListPage(string? page, CatalogTourListSort sort, bool descending, CancellationToken ct);
}
