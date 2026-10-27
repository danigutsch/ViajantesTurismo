namespace ViajantesTurismo.Architecture.Analyzers;

/// <summary>
/// Exposes diagnostic identifiers for ViajantesTurismo architecture policy.
/// </summary>
public static class ArchitectureDiagnosticIds
{
    /// <summary>
    /// Diagnostic emitted for ordinary HTTP communication between production API services.
    /// </summary>
    public const string ProductionApiHttp = "VTARCH001";

    /// <summary>
    /// Diagnostic emitted for an integration event whose name does not end with IntegrationEvent.
    /// </summary>
    public const string IntegrationEventName = "VTARCH002";

    /// <summary>
    /// Diagnostic emitted for a domain error class whose name does not end with Errors.
    /// </summary>
    public const string ErrorClassName = "VTARCH003";
}
