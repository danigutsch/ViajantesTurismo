using System.Diagnostics.CodeAnalysis;
using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

/// <summary>
/// Carries a complete Branding settings revision, including the re-encoded logo bytes, to downstream projections.
/// The logo fields are additive and nullable, so version 1 stays compatible: events published before they existed
/// deserialize without a logo.
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
/// <param name="LogoUri">The optional app-generated root-relative public logo URL.</param>
/// <param name="LogoSha256">The lowercase hexadecimal SHA-256 of <paramref name="LogoContent" />, or null without a logo.</param>
/// <param name="LogoContentType">The media type of <paramref name="LogoContent" />, or null without a logo.</param>
/// <param name="LogoContent">The re-encoded logo bytes, or null without a logo.</param>
[SuppressMessage(
    "Performance",
    "CA1819:Properties should not return arrays",
    Justification = "The serialized contract carries the re-encoded logo bytes as one immutable base64 payload.")]
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
    Uri? LogoUri,
    string? LogoSha256 = null,
    string? LogoContentType = null,
    byte[]? LogoContent = null) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "branding.settings.changed";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
