using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

/// <summary>
/// Represents the duplicate-resolution state for a single conflicting customer import row.
/// </summary>
internal sealed class ImportCustomerConflictState
{
    private readonly Dictionary<string, ImportConflictFieldSource> _fieldSelections = new(StringComparer.OrdinalIgnoreCase);

    internal ImportCustomerConflictState(
        string email,
        IReadOnlyDictionary<string, string>? incomingValues,
        IReadOnlyDictionary<string, string>? existingValues)
    {
        Email = email;
        IncomingValues = new Dictionary<string, string>(incomingValues ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
        ExistingValues = new Dictionary<string, string>(existingValues ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
    }

    internal string Email { get; }

    internal string? Decision { get; private set; }

    internal Dictionary<string, string> IncomingValues { get; }

    internal Dictionary<string, string> ExistingValues { get; }

    internal Dictionary<string, ImportConflictFieldSource> FieldSelections => _fieldSelections;

    internal bool IsResolved => !string.IsNullOrWhiteSpace(Decision);

    internal bool HasMixedDecision => string.Equals(Decision, "mixed", StringComparison.OrdinalIgnoreCase);

    internal void SetDecision(string decision, IReadOnlyList<CustomerImportFieldMapping> fieldMappings)
    {
        SetDecision(decision, fieldMappings.Select(mapping => mapping.Field.Name));
    }

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
            if (_fieldSelections.ContainsKey(fieldName))
            {
                continue;
            }

            _fieldSelections[fieldName] = ExistingValues.ContainsKey(fieldName)
                ? ImportConflictFieldSource.Existing
                : ImportConflictFieldSource.Incoming;
        }
    }

    internal void SetFieldSource(string fieldName, ImportConflictFieldSource source)
    {
        _fieldSelections[fieldName] = source;
    }

    internal string GetIncomingValue(string fieldName) => IncomingValues.GetValueOrDefault(fieldName, string.Empty);

    internal string GetExistingValue(string fieldName) => ExistingValues.GetValueOrDefault(fieldName, string.Empty);
}
