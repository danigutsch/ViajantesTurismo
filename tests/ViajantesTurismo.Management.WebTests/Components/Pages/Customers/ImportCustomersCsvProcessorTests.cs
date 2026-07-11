using System.Text;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersCsvProcessorTests
{
    [Fact]
    public void BuildImportSummary_when_result_and_conflict_decisions_are_provided_returns_expected_counts()
    {
        // Arrange
        var result = new ImportResultDto(3, 2);
        var keepState = new ImportCustomerConflictState("keep@example.com", null, null);
        keepState.SetDecision("keep", CustomerImportHeaderMatcher.Fields);

        var overwriteState = new ImportCustomerConflictState("overwrite@example.com", null, null);
        overwriteState.SetDecision("overwrite", CustomerImportHeaderMatcher.Fields);

        var mixedState = new ImportCustomerConflictState("mixed@example.com", null, null);
        mixedState.SetDecision("mixed", CustomerImportHeaderMatcher.Fields);

        var conflictStates = new[] { keepState, overwriteState, mixedState };

        // Act
        var summary = ImportCustomersCsvProcessor.BuildImportSummary(result, conflictStates);

        // Assert
        (summary.CreatedCount).ShouldBe(1);
        (summary.UpdatedCount).ShouldBe(2);
        (summary.SkippedCount).ShouldBe(1);
        (summary.FailedCount).ShouldBe(2);
    }

    [Fact]
    public void BuildErrorReportDataUri_when_error_rows_have_special_characters_escapes_csv_content()
    {
        // Arrange
        var errorRows = new List<ImportErrorRowDto>
        {
            new(4, "First,Name", "Value \"quoted\"\nand wrapped", "bad@example.com"),
        };

        // Act
        var dataUri = ImportCustomersCsvProcessor.BuildErrorReportDataUri(errorRows);

        // Assert
        _ = (dataUri).ShouldNotBeNull();
        var csvPayload = Uri.UnescapeDataString(dataUri.Split(',', 2)[1]);
        (csvPayload).ShouldContain("LineNumber,Field,Message,Email", StringComparison.Ordinal);
        (csvPayload).ShouldContain("\"First,Name\"", StringComparison.Ordinal);
        (csvPayload).ShouldContain("\"Value \"\"quoted\"\"", StringComparison.Ordinal);
        (csvPayload).ShouldContain("bad@example.com", StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMixedFieldSelections_when_existing_source_is_selected_uses_existing_field_value()
    {
        // Arrange
        var mappedCsv = Encoding.UTF8.GetBytes(CustomerImportCsvTestData.AllCanonicalHeaders + "\n" + CustomerImportCsvTestData.BuildCsvRow(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"] = "IncomingFirst",
            ["LastName"] = "IncomingLast",
            ["Email"] = "mixed@example.com",
        }));

        var mixedState = new ImportCustomerConflictState(
            "mixed@example.com",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "IncomingFirst",
                ["LastName"] = "IncomingLast",
                ["Email"] = "mixed@example.com",
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["LastName"] = "ExistingLast",
            });
        mixedState.SetDecision("mixed", CustomerImportHeaderMatcher.Fields);
        mixedState.SetFieldSource("FirstName", ImportConflictFieldSource.Incoming);
        mixedState.SetFieldSource("LastName", ImportConflictFieldSource.Existing);

        // Act
        var mergedBytes = ImportCustomersCsvProcessor.ApplyMixedFieldSelections(
            mappedCsv,
            [mixedState]);

        // Assert
        var committedCsv = Encoding.UTF8.GetString(mergedBytes);
        var committedLines = committedCsv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        (committedLines.Length).ShouldBe(2);

        var committedHeaders = committedLines[0].Split(',');
        var committedValues = committedLines[1].Split(',');
        var headerIndexes = committedHeaders
            .Select((header, index) => new { header, index })
            .ToDictionary(item => item.header, item => item.index, StringComparer.Ordinal);

        (committedValues[headerIndexes["FirstName"]]).ShouldBe("IncomingFirst");
        (committedValues[headerIndexes["LastName"]]).ShouldBe("ExistingLast");
        (committedValues[headerIndexes["Email"]]).ShouldBe("mixed@example.com");
    }
}
