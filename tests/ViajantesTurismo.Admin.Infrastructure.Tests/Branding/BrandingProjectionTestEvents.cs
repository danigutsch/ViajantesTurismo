using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Branding;

internal static class BrandingProjectionTestEvents
{
    public static readonly Guid Epoch = Guid.Parse("0199f0aa-0000-7000-8000-000000000002");

    public static BrandingSettingsChangedIntegrationEvent Create(long revision, string brandName) => new(
        Guid.CreateVersion7(),
        new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero),
        Epoch,
        revision,
        brandName,
        "#0F766E",
        "#F97316",
        "#FFFBF5",
        "#1F2937",
        "Georgia",
        "system-ui",
        LogoUri: null);
}
