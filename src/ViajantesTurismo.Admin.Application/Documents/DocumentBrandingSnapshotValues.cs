namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Carries captured branding values to the travel-contract aggregate.
/// </summary>
internal sealed record DocumentBrandingSnapshotValues(string Version, string BrandName, Uri? LogoUri);
