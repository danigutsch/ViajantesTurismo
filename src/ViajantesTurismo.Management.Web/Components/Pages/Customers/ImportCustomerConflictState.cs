using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Represents the duplicate-resolution state for a single conflicting customer import row.
/// </summary>
internal sealed class ImportCustomerConflictState
{
    /// <summary>Creates the state from the conflicting email and the incoming and existing field values; null values become empty.</summary>
    internal ImportCustomerConflictState(
        string email,
        IReadOnlyDictionary<string, string>? incomingValues,
        IReadOnlyDictionary<string, string>? existingValues)
    {
        Email = email;
        IncomingValues = new Dictionary<string, string>(incomingValues ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        ExistingValues = new Dictionary<string, string>(existingValues ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Email address that identifies the conflicting customer.</summary>
    internal string Email
    {
        get;
    }

    /// <summary>Chosen resolution (keep, overwrite or mixed), or null while unresolved.</summary>
    internal string? Decision
    {
        get; private set;
    }

    /// <summary>Field values from the imported file, keyed by field name.</summary>
    internal Dictionary<string, string> IncomingValues
    {
        get;
    }

    /// <summary>Field values already stored for the customer, keyed by field name.</summary>
    internal Dictionary<string, string> ExistingValues
    {
        get;
    }

    /// <summary>Per-field source choice used when the decision is mixed.</summary>
    internal Dictionary<string, ImportConflictFieldSource> FieldSelections { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether a decision has been chosen.</summary>
    internal bool IsResolved => !string.IsNullOrWhiteSpace(Decision);

    /// <summary>Whether the decision is mixed, merging field by field.</summary>
    internal bool HasMixedDecision => string.Equals(Decision, "mixed", StringComparison.OrdinalIgnoreCase);

    /// <summary>Sets the decision and, when mixed, seeds field selections from the mapped fields.</summary>
    internal void SetDecision(string decision, IReadOnlyList<CustomerImportFieldMapping> fieldMappings)
    {
        SetDecision(decision, fieldMappings.Select(mapping => mapping.Field.Name));
    }

    /// <summary>Sets the decision and, when mixed, seeds field selections from the import fields.</summary>
    internal void SetDecision(string decision, IReadOnlyList<CustomerImportField> fields)
    {
        SetDecision(decision, fields.Select(field => field.Name));
    }

    private void SetDecision(string decision, IEnumerable<string> fieldNames)
    {
        Decision = decision;
        if (HasMixedDecision)
        {
            EnsureMixedSelections(fieldNames);
        }
    }

    private void EnsureMixedSelections(IEnumerable<string> fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (FieldSelections.ContainsKey(fieldName))
            {
                continue;
            }

            FieldSelections[fieldName] = ExistingValues.ContainsKey(fieldName)
                ? ImportConflictFieldSource.Existing
                : ImportConflictFieldSource.Incoming;
        }
    }

    /// <summary>Chooses which source supplies the value for the field in a mixed merge.</summary>
    internal void SetFieldSource(string fieldName, ImportConflictFieldSource source)
    {
        FieldSelections[fieldName] = source;
    }

    /// <summary>Returns the incoming value for the field, or an empty string when absent.</summary>
    internal string GetIncomingValue(string fieldName) => IncomingValues.GetValueOrDefault(fieldName, string.Empty);

    /// <summary>Returns the existing value for the field, or an empty string when absent.</summary>
    internal string GetExistingValue(string fieldName) => ExistingValues.GetValueOrDefault(fieldName, string.Empty);
}
