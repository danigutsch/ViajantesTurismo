namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Identifies the intended audience for a generated document.
/// </summary>
public enum DocumentAudience
{
    /// <summary>
    /// Customer-facing content.
    /// </summary>
    Customer = 0,

    /// <summary>
    /// Internal staff content.
    /// </summary>
    Staff = 1
}
