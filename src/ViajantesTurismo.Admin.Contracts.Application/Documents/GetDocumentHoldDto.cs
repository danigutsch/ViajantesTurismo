namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents the Admin-safe read model of one disposition hold on a document lineage: opaque codes and dates only.</summary>
public sealed record GetDocumentHoldDto
{
    /// <summary>Gets the opaque hold identifier.</summary>
    public required Guid Id
    {
        get; init;
    }

    /// <summary>Gets why the hold exists.</summary>
    public required string Basis
    {
        get; init;
    }

    /// <summary>Gets the opaque purpose code.</summary>
    public required string PurposeCode
    {
        get; init;
    }

    /// <summary>Gets the opaque reference to the authorizing record.</summary>
    public required string AuthorityReference
    {
        get; init;
    }

    /// <summary>Gets the opaque identifier of the actor that placed the hold.</summary>
    public required string PlacedBy
    {
        get; init;
    }

    /// <summary>Gets when the hold was placed.</summary>
    public required DateTimeOffset PlacedAt
    {
        get; init;
    }

    /// <summary>Gets when the hold starts to restrain disposition.</summary>
    public required DateTimeOffset EffectiveFrom
    {
        get; init;
    }

    /// <summary>Gets when the hold is next due for review.</summary>
    public required DateTimeOffset ReviewDueAt
    {
        get; init;
    }

    /// <summary>Gets when the hold stops restraining disposition by itself, or <see langword="null" />.</summary>
    public DateTimeOffset? ExpiresAt
    {
        get; init;
    }

    /// <summary>Gets the opaque identifier of the actor that released the hold, or <see langword="null" />.</summary>
    public string? ReleasedBy
    {
        get; init;
    }

    /// <summary>Gets when the hold was released, or <see langword="null" />.</summary>
    public DateTimeOffset? ReleasedAt
    {
        get; init;
    }

    /// <summary>Gets the opaque reason code recorded at release, or <see langword="null" />.</summary>
    public string? ReleaseReasonCode
    {
        get; init;
    }

    /// <summary>Gets whether the hold restrains disposition at the time of the response.</summary>
    public required bool IsRestraining
    {
        get; init;
    }
}
