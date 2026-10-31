using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Builds the settings-changed event and its previous byte-array twin for the wire-format tests.</summary>
internal static class BrandingLogoWireFormatData
{
    /// <summary>The base64 text of <see cref="LogoBytes" />.</summary>
    public const string LogoBase64 = "UklGRgwAAABXRUJQVlA4IAECAwQ=";

    /// <summary>A fixed event id.</summary>
    public static readonly Guid EventId = Guid.Parse("0199f0aa-0000-7000-8000-0000000000aa");
    /// <summary>A fixed source epoch.</summary>
    public static readonly Guid Epoch = Guid.Parse("0199f0aa-0000-7000-8000-000000000001");
    /// <summary>A fixed commit instant.</summary>
    public static readonly DateTimeOffset OccurredAt = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    /// <summary>A WebP-signed payload.</summary>
    public static readonly byte[] LogoBytes =
    [
        0x52, 0x49, 0x46, 0x46, 0x0C, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38, 0x20, 0x01, 0x02, 0x03, 0x04
    ];

    /// <summary>Builds the event with the given logo bytes, or without a logo.</summary>
    public static BrandingSettingsChangedIntegrationEvent CreateEvent(ReadOnlyMemory<byte>? logo) => new(
        EventId,
        OccurredAt,
        Epoch,
        4,
        "Viajantes",
        "#0F766E",
        "#F97316",
        "#FFFBF5",
        "#1F2937",
        "Georgia",
        "system-ui",
        new Uri("/logo.svg", UriKind.Relative),
        logo is null ? null : "sha",
        logo is null ? null : "image/webp",
        logo);

    /// <summary>Builds the previous byte-array payload with the same values.</summary>
    public static LegacyByteArrayBrandingPayload CreatePrevious(byte[]? logo) => new(
        EventId,
        OccurredAt,
        Epoch,
        4,
        "Viajantes",
        "#0F766E",
        "#F97316",
        "#FFFBF5",
        "#1F2937",
        "Georgia",
        "system-ui",
        new Uri("/logo.svg", UriKind.Relative),
        logo is null ? null : "sha",
        logo is null ? null : "image/webp",
        logo);
}
