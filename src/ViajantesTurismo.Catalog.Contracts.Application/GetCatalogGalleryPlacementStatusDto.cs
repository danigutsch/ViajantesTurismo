using System.Text.Json.Serialization;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Contains the retained status metadata for a single accepted Gallery placement.</summary>
/// <param name="CatalogTourId">The tour owning the placement.</param>
/// <param name="Sequence">The accepted per-tour placement sequence.</param>
/// <param name="IntentId">The durable placement intent identifier.</param>
/// <param name="Status">The committed placement state.</param>
/// <param name="Revision">The terminal stream revision, when recorded.</param>
/// <param name="AcceptedAt">The original acceptance time, when recorded.</param>
/// <param name="TerminalAt">The committed terminal transition time, when recorded.</param>
public sealed record GetCatalogGalleryPlacementStatusDto(
    Guid CatalogTourId, long Sequence, Guid IntentId, CatalogGalleryPlacementStatusDto Status,
    long? Revision, DateTimeOffset? AcceptedAt, DateTimeOffset? TerminalAt)
{
    /// <summary>The number of publication failures that makes a due wake a repeated retry.</summary>
    public const int MinimumRepeatedWakeFailures = 2;

    /// <summary>Gets the committed event that applied, rejected, or closed the placement.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? TerminalEventId
    {
        get; init;
    }

    /// <summary>Gets the persisted cause of the terminal transition, when known.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CausationEventId
    {
        get; init;
    }

    /// <summary>Gets the verified Catalog wake identity independently of retained delivery metadata.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? TriggeringWakeEventId
    {
        get; init;
    }

    /// <summary>Gets the recorded Catalog withdrawal for the matching retirement, when available.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CatalogGalleryRetirementDto? Retirement
    {
        get; init;
    }

    /// <summary>Gets retained delivery metadata for the wake queued when this placement was accepted.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CatalogGalleryOriginalWakeDeliveryDto? OriginalWake
    {
        get; init;
    }

    /// <summary>Gets retained delivery metadata for the Catalog wake that caused the committed transition.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CatalogGalleryOriginalWakeDeliveryDto? TriggeringWake
    {
        get; init;
    }

    /// <summary>Gets pending delivery metadata for the original wake and any associated bootstrap wake.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CatalogGalleryPendingWakeDeliveryDto>? RelatedPendingWakes
    {
        get; init;
    }

    /// <summary>Checks whether an associated pending wake has repeatedly failed and its recorded retry is due.</summary>
    /// <param name="observedAt">The time at which the placement is being inspected.</param>
    /// <returns>Whether the pending placement has a repeatedly failed wake whose retry is due.</returns>
    public bool HasRepeatedWakeRetryDue(DateTimeOffset observedAt) =>
        Status == CatalogGalleryPlacementStatusDto.Pending
        && RelatedPendingWakes?.Any(wake => wake is { FailedAttempts: >= MinimumRepeatedWakeFailures, NextAttemptAt: { } nextAttempt }
            && nextAttempt <= observedAt) == true;
}
