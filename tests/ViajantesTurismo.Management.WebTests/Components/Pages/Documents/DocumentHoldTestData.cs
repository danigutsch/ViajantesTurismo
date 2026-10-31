namespace ViajantesTurismo.Management.WebTests.Components.Pages.Documents;

/// <summary>Synthetic hold data: opaque codes and dates only, never document content or personal data.</summary>
internal static class DocumentHoldTestData
{
    /// <summary>Builds a restraining hold unless a release or review date makes it otherwise.</summary>
    public static GetDocumentHoldDto Create(
        string basis = "LegalClaim",
        string purposeCode = "PURPOSE-1",
        string authorityReference = "AUTH-1",
        bool released = false,
        DateTimeOffset? reviewDueAt = null)
    {
        var now = TimeProvider.System.GetUtcNow();
        return new GetDocumentHoldDto
        {
            Id = Guid.CreateVersion7(),
            Basis = basis,
            PurposeCode = purposeCode,
            AuthorityReference = authorityReference,
            PlacedBy = "actor-place",
            PlacedAt = now.AddDays(-5),
            EffectiveFrom = now.AddDays(-5),
            ReviewDueAt = reviewDueAt ?? now.AddDays(60),
            ReleasedBy = released ? "actor-release" : null,
            ReleasedAt = released ? now.AddDays(-1) : null,
            ReleaseReasonCode = released ? "CLAIM-CLOSED" : null,
            IsRestraining = !released
        };
    }

    /// <summary>Builds a first overview page of the default size.</summary>
    public static GetDocumentHoldOverviewPageDto OverviewPage(IReadOnlyList<GetDocumentHoldOverviewItemDto> items, bool hasMore) =>
        new()
        {
            Items = items,
            Page = 1,
            PageSize = 25,
            HasMore = hasMore
        };
}
