namespace ViajantesTurismo.Branding.Contracts.Http;

/// <summary>One management read: the settings document and the validator that guards the next write.</summary>
/// <param name="Settings">The settings document.</param>
/// <param name="EntityTag">The quoted strong entity tag returned with the document.</param>
public sealed record BrandingManagementDocument(BrandingManagementSettingsDto Settings, string EntityTag);
