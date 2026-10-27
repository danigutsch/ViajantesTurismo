namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Identifies which source value should win during a mixed conflict merge.
/// </summary>
internal enum ImportConflictFieldSource
{
    /// <summary>Keep the value already stored for the customer.</summary>
    Existing,
    /// <summary>Use the value from the imported file.</summary>
    Incoming,
}
