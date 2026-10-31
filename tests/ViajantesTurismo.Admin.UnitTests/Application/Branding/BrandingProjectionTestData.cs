using System.Security.Cryptography;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

/// <summary>Builds branding projection events, authorizations and reseed requests for tests.</summary>
internal static class BrandingProjectionTestData
{
    /// <summary>Default source epoch used by test events.</summary>
    public static readonly Guid Epoch = Guid.Parse("0199f0aa-0000-7000-8000-000000000001");

    /// <summary>Builds a branding settings changed event with the given revision, defaulting epoch and event id.</summary>
    public static BrandingSettingsChangedIntegrationEvent CreateEvent(
        long revision,
        string brandName = "Viajantes",
        Guid? epoch = null,
        Guid? eventId = null,
        string? logoSha256 = null,
        string? logoContentType = null,
        ReadOnlyMemory<byte>? logoContent = null) => new(
        eventId ?? Guid.CreateVersion7(),
        new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero),
        epoch ?? Epoch,
        revision,
        brandName,
        "#0F766E",
        "#F97316",
        "#FFFBF5",
        "#1F2937",
        "Georgia",
        "system-ui",
        new Uri("/logo.svg", UriKind.Relative),
        logoSha256,
        logoContentType,
        logoContent);

    /// <summary>Builds an event carrying a complete, valid logo.</summary>
    public static BrandingSettingsChangedIntegrationEvent CreateEventWithLogo(long revision, byte[] content, Guid? epoch = null, Guid? eventId = null) =>
        CreateEvent(
            revision,
            epoch: epoch,
            eventId: eventId,
            logoSha256: Convert.ToHexStringLower(SHA256.HashData(content)),
            logoContentType: "image/webp",
            logoContent: content);

    /// <summary>Builds a reseed authorization matching the given reseed event.</summary>
    public static BrandingReseedAuthorization CreateAuthorization(BrandingSettingsChangedIntegrationEvent reseed) => new(
        reseed.EventId,
        reseed.SourceEpoch,
        reseed.SourceRevision,
        "restore after incident",
        "INC-42",
        "operator@example.test",
        new DateTimeOffset(2026, 10, 24, 13, 0, 0, TimeSpan.Zero));

    /// <summary>Builds a reseed request with the given expected and target epoch and revision.</summary>
    public static BrandingReseedRequest CreateReseedRequest(
        Guid? expectedEpoch,
        long? expectedRevision,
        Guid targetEpoch,
        long targetRevision) =>
        new(expectedEpoch, expectedRevision, Guid.CreateVersion7(), targetEpoch, targetRevision, "restore", "INC-42", "operator@example.test");
}
