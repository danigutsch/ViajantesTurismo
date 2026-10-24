namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Selects where document generation captures Branding from.</summary>
public enum DocumentBrandingSourceMode
{
    /// <summary>
    /// Captures Branding over HTTP and compares it with the local projection (shadow verification).
    /// </summary>
    Http = 0,

    /// <summary>Captures Branding only from the local Admin projection and fails closed when it is missing.</summary>
    Projection = 1
}
