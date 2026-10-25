using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Import;

namespace ViajantesTurismo.Admin.UnitTests.Application.Customers.Import;

public sealed class CsvErrorCatalogTests
{
    [Fact]
    public void Each_catalog_entry_is_an_invalid_result_with_its_documented_field_and_message()
    {
        // Arrange
        (Result Error, string Detail, string Field, string Message)[] entries =
        [
            (CsvErrors.RequiredHeaderMissing("Email"), "Required header 'Email' is missing.", "headers", "Required header missing."),
            (CsvErrors.RowsHaveInconsistentColumnCounts(), "All rows must have the same number of columns as there are headers.", "rows", "Inconsistent column counts detected."),
            (CsvErrors.RowsHaveInconsistentColumnCounts(7), "All rows must have the same number of columns as there are headers (line 7).", "rows", "Inconsistent column counts detected."),
            (CsvErrors.HeadersMustContainAtLeastOneColumn(), "Headers must contain at least one column.", "headers", "Missing headers."),
            (CsvErrors.HeaderCountMustMatchRowColumnCount(3), "Header count must match row column count (line 3).", "headers", "Header and row column counts do not match."),
        ];

        // Act and assert
        foreach (var (error, detail, field, message) in entries)
        {
            error.Status.ShouldBe(ResultStatus.Invalid);
            var details = error.ErrorDetails.ShouldNotBeNull();
            details.Detail.ShouldBe(detail);
            details.ValidationErrors.ShouldNotBeNull()[field].ShouldHaveSingleItem().ShouldBe(message);
        }
    }

    [Fact]
    public void Csv_rows_with_different_text_are_not_equal()
    {
        // Assert
        CsvRow.Parse("a,b").ShouldNotBe(CsvRow.Parse("a,c"));
    }
}
