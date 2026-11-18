namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

public sealed record CustomerImportFieldMapping(CustomerImportField Field, string? MatchedCsvHeader)
{
    public bool IsAutoMatched => MatchedCsvHeader is not null;
}
