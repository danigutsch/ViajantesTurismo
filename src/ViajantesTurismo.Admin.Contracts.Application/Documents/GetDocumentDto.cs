namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents the Admin-safe read model for one generated document revision.</summary>
public sealed record GetDocumentDto
{
    /// <summary>Gets the opaque document revision identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the source booking identifier.</summary>
    public required Guid BookingId { get; init; }

    /// <summary>Gets the revision number within the document lineage.</summary>
    public required int Revision { get; init; }

    /// <summary>Gets the server-selected template identifier.</summary>
    public required string TemplateId { get; init; }

    /// <summary>Gets the server-selected template version.</summary>
    public required string TemplateVersion { get; init; }

    /// <summary>Gets the deterministic source-data version signal.</summary>
    public required string SourceVersion { get; init; }

    /// <summary>Gets the current document lifecycle status.</summary>
    public required DocumentStatusDto Status { get; init; }

    /// <summary>Gets the fields visible to authorized Admin staff.</summary>
    public required IReadOnlyList<GetDocumentFieldDto> Fields { get; init; }

    /// <summary>Gets when this revision was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Gets when this revision was last changed.</summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>Gets when this revision was finalized, when available.</summary>
    public DateTime? FinalizedAt { get; init; }

    /// <summary>Gets the previous revision replaced by this revision, when available.</summary>
    public Guid? ReplacesDocumentId { get; init; }

    /// <summary>Gets whether a finalized artifact is available for mediated download.</summary>
    public required bool HasFinalizedArtifact { get; init; }
}
