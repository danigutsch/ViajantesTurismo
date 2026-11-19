namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>A source column that may be suitable for an import field, with a user-facing explanation.</summary>
/// <param name="Header">The source column name as shown in the CSV.</param>
/// <param name="Reason">Why the column may match the import field.</param>
internal sealed record CustomerImportColumnSuggestion(string Header, string Reason);
