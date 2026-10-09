using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

/// <summary>
/// Carries a complete public-safe Branding settings revision to downstream projections.
/// </summary>
/// <param name="EventId">The stable event identifier used for idempotent consumption.</param>
/// <param name="OccurredAt">The UTC instant when the revision was committed.</param>
/// <param name="SourceEpoch">The identifier of the authoritative source lineage.</param>
/// <param name="SourceRevision">The positive monotonic revision within the source epoch.</param>
/// <param name="BrandName">The display brand name.</param>
/// <param name="PrimaryColor">The primary CSS color.</param>
/// <param name="AccentColor">The accent CSS color.</param>
/// <param name="BackgroundColor">The background CSS color.</param>
/// <param name="TextColor">The text CSS color.</param>
/// <param name="HeadingFontFamily">The heading font family.</param>
/// <param name="BodyFontFamily">The body font family.</param>
/// <param name="LogoUri">The optional public logo URI.</param>
public sealed record BrandingSettingsChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SourceEpoch,
    long SourceRevision,
    string BrandName,
    string PrimaryColor,
    string AccentColor,
    string BackgroundColor,
    string TextColor,
    string HeadingFontFamily,
    string BodyFontFamily,
    Uri? LogoUri) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "branding.settings.changed";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
