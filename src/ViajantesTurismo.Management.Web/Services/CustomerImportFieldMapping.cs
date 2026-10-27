namespace ViajantesTurismo.Management.Web.Services;

public sealed record CustomerImportFieldMapping(CustomerImportField Field, string? MatchedCsvHeader)
{
    public bool IsAutoMatched => MatchedCsvHeader is not null;
}
