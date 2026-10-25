using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.UnitTests.Application.Branding;

internal static class BrandingProjectionTestData
{
    public static readonly Guid Epoch = Guid.Parse("0199f0aa-0000-7000-8000-000000000001");

    public static BrandingSettingsChangedIntegrationEvent CreateEvent(
        long revision,
        string brandName = "Viajantes",
        Guid? epoch = null,
        Guid? eventId = null) => new(
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
        new Uri("/logo.svg", UriKind.Relative));

    public static BrandingReseedAuthorization CreateAuthorization(BrandingSettingsChangedIntegrationEvent reseed) => new(
        reseed.EventId,
        reseed.SourceEpoch,
        reseed.SourceRevision,
        "restore after incident",
        "INC-42",
        "operator@example.test",
        new DateTimeOffset(2026, 10, 24, 13, 0, 0, TimeSpan.Zero));

    public static BrandingReseedRequest CreateReseedRequest(
        Guid? expectedEpoch,
        long? expectedRevision,
        Guid targetEpoch,
        long targetRevision) =>
        new(expectedEpoch, expectedRevision, Guid.CreateVersion7(), targetEpoch, targetRevision, "restore", "INC-42", "operator@example.test");
}
