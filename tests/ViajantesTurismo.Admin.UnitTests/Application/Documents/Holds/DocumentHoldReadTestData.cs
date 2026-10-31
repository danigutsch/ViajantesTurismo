using PandaBear.DispositionHold;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.UnitTests.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Application.Documents.Holds;

/// <summary>Builds holds and overview queries for the hold read tests.</summary>
internal static class DocumentHoldReadTestData
{
    /// <summary>Stores a hold on a lineage placed ten days before the harness clock unless told otherwise.</summary>
    public static Hold AddHold(DocumentHoldHarness harness, Guid lineageId, DateTimeOffset reviewDueAt, HoldBasis basis = HoldBasis.LegalClaim, DateTimeOffset? placedAt = null)
    {
        var placed = placedAt ?? harness.Now.AddDays(-10);
        var hold = Hold.Place(DocumentHoldScopes.ForLineage(lineageId), basis, "purpose.claim-001", "authority:case-0001", "actor-0001", placed, placed, reviewDueAt);
        harness.Store.Holds.Add(hold);
        return hold;
    }

    /// <summary>Creates an overview query with first-page defaults.</summary>
    public static ListDocumentHoldOverviewQuery Query(DocumentHoldState? state = null, HoldBasis? basis = null, DateTimeOffset? releasedSince = null, int page = 1, int pageSize = 25) =>
        new(state, basis, releasedSince, page, pageSize);

    /// <summary>Runs the overview for one state.</summary>
    public static Task<Result<GetDocumentHoldOverviewPageDto>> List(DocumentHoldHarness harness, DocumentHoldState state) =>
        harness.Overview.Handle(Query(state), TestContext.Current.CancellationToken);
}
