using PandaBear.DispositionHold;

namespace ViajantesTurismo.Admin.Application.Documents.Holds;

/// <summary>Requests one page of holds across all document lineages.</summary>
/// <param name="State">Keeps only holds in this state, or every hold when <see langword="null" />.</param>
/// <param name="Basis">Keeps only holds with this basis, or every basis when <see langword="null" />.</param>
/// <param name="ReleasedSince">Keeps only holds released at or after this instant, or ignores release time when <see langword="null" />.</param>
/// <param name="Page">The one-based page number.</param>
/// <param name="PageSize">The maximum number of holds per page.</param>
public sealed record ListDocumentHoldOverviewQuery(DocumentHoldState? State, HoldBasis? Basis, DateTimeOffset? ReleasedSince, int Page, int PageSize);
