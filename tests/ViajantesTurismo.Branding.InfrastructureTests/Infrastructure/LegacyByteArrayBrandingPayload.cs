using System.Text.Json.Serialization;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>The settings-changed payload as it was published when the logo was a <c>byte[]</c>; the wire format reference.</summary>
/// <param name="EventId">The EventId field.</param>
/// <param name="OccurredAt">The OccurredAt field.</param>
/// <param name="SourceEpoch">The SourceEpoch field.</param>
/// <param name="SourceRevision">The SourceRevision field.</param>
/// <param name="BrandName">The BrandName field.</param>
/// <param name="PrimaryColor">The PrimaryColor field.</param>
/// <param name="AccentColor">The AccentColor field.</param>
/// <param name="BackgroundColor">The BackgroundColor field.</param>
/// <param name="TextColor">The TextColor field.</param>
/// <param name="HeadingFontFamily">The HeadingFontFamily field.</param>
/// <param name="BodyFontFamily">The BodyFontFamily field.</param>
/// <param name="LogoUri">The LogoUri field.</param>
/// <param name="LogoSha256">The LogoSha256 field.</param>
/// <param name="LogoContentType">The LogoContentType field.</param>
/// <param name="LogoContent">The LogoContent field.</param>
internal sealed record LegacyByteArrayBrandingPayload(
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
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LogoSha256 = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LogoContentType = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] byte[]? LogoContent = null);
