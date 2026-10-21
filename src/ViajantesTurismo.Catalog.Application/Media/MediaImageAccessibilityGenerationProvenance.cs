namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>Identifies configured generation inputs without claiming the provider's routed model.</summary>
/// <param name="Provider">The configured adapter provider.</param>
/// <param name="RequestedModel">The configured model identifier requested from the adapter.</param>
/// <param name="PromptVersion">The source artifact version containing the adapter's prompt implementation.</param>
/// <param name="ConfigurationVersion">The fingerprint of non-secret generation configuration.</param>
public sealed record MediaImageAccessibilityGenerationProvenance(string Provider, string RequestedModel, string PromptVersion, string ConfigurationVersion);
