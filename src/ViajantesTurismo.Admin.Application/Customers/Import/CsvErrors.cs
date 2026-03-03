using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Application.Customers.Import;

/// <summary>
/// Defines common error results for CSV document processing, such as inconsistent column counts across rows.
/// </summary>
public static class CsvErrors
{
    /// <summary>
    /// Result indicating that all rows must have the same number of columns as there are headers.
    /// </summary>
    public static Result<CsvDocument> RowsHaveInconsistentColumnCounts() =>
        Result<CsvDocument>.Invalid(
            detail: "All rows must have the same number of columns as there are headers.",
            field: "rows",
            message: "Inconsistent column counts detected."
        );

    /// <summary>
    /// Result indicating that CSV headers must contain at least one column.
    /// </summary>
    public static Result<CsvDocument> HeadersMustContainAtLeastOneColumn() =>
        Result<CsvDocument>.Invalid(
            detail: "Headers must contain at least one column.",
            field: "headers",
            message: "Missing headers."
        );

    /// <summary>
    /// Result indicating that header count must match row column count.
    /// </summary>
    public static Result<CsvDocument> HeaderCountMustMatchRowColumnCount() =>
        Result<CsvDocument>.Invalid(
            detail: "Header count must match row column count.",
            field: "headers",
            message: "Header and row column counts do not match."
        );
}
