namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

/// <summary>
/// Represents a single duplicate-resolution row rendered by the customer import UI.
/// </summary>
public sealed class ImportCustomersDuplicateResolutionItem
{
    /// <summary>
    /// Gets or sets the conflicting customer email.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets the selected resolution decision.
    /// </summary>
    public string? Decision { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the conflict has a selected decision.
    /// </summary>
    public bool IsResolved { get; init; }

    /// <summary>
    /// Gets or sets the field source selections for mixed merges.
    /// </summary>
    public IReadOnlyDictionary<string, bool> UseExistingFieldSelections { get; init; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the existing field values loaded from the current customer record.
    /// </summary>
    public IReadOnlyDictionary<string, string> ExistingValues { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the incoming field values parsed from the import file.
    /// </summary>
    public IReadOnlyDictionary<string, string> IncomingValues { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
