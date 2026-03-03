using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Defines common error results for CSV document processing, such as inconsistent column counts across rows.
/// </summary>
public static class CsvErrors
{
    /// <summary>
    /// Result indicating that all rows must have the same number of columns.
    /// </summary>
    public static Result<CsvDocument> RowsHaveInconsistentColumnCounts() =>
        Result<CsvDocument>.Invalid(
            detail: "All rows must have the same number of columns.",
            field: "rows",
            message: "Inconsistent column counts detected."
        );
}
