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
}
