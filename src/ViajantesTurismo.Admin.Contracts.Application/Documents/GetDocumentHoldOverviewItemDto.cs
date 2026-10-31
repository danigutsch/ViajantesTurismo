namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents one hold in the cross-document overview: the opaque lineage it restrains and the hold itself.</summary>
public sealed record GetDocumentHoldOverviewItemDto
{
    /// <summary>Gets the opaque identifier of the document lineage the hold restrains.</summary>
    public required Guid LineageId
    {
        get; init;
    }

    /// <summary>Gets the Admin-safe read model of the hold.</summary>
    public required GetDocumentHoldDto Hold
    {
        get; init;
    }
}
