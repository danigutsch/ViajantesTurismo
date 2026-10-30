using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Builds branding settings changed integration events for projection tests.</summary>
internal static class BrandingProjectionTestEvents
{
    /// <summary>Default epoch used by events built with <see cref="Create" />.</summary>
    public static readonly Guid Epoch = Guid.Parse("0199f0aa-0000-7000-8000-000000000002");

    /// <summary>Creates a branding settings changed event with fixed colors and fonts for the given revision and brand name; the epoch defaults to <see cref="Epoch" />.</summary>
    public static BrandingSettingsChangedIntegrationEvent Create(long revision, string brandName, Guid? epoch = null) => new(
        Guid.CreateVersion7(),
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
        LogoUri: null);
}
