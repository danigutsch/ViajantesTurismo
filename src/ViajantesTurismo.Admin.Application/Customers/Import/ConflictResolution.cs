namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Represents the decision to apply when an imported row conflicts with an existing customer.
/// </summary>
/// <param name="PreservesExistingCustomer">Indicates whether the existing customer data must be preserved.</param>
/// <param name="SkipsImport">Indicates whether the incoming row must be skipped.</param>
public readonly record struct ConflictResolution(bool PreservesExistingCustomer, bool SkipsImport)
{
    /// <summary>
    /// Keeps existing customer data and skips importing the conflicting row.
    /// </summary>
    public static ConflictResolution Keep => new(true, true);

    /// <summary>
    /// Overwrites existing customer data using incoming data.
    /// </summary>
    public static ConflictResolution Overwrite => new(false, false);
}
