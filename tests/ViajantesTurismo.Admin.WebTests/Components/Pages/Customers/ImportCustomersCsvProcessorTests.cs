using System.Text;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class ImportCustomersCsvProcessorTests
{
    private static readonly string AllCanonicalHeaders =
        string.Join(",", CustomerImportHeaderMatcher.Fields.Select(f => f.Name));

    [Fact]
    public void BuildImportSummary_When_Result_And_Conflict_Decisions_Are_Provided_Returns_Expected_Counts()
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
        Assert.Equal(1, summary.CreatedCount);
        Assert.Equal(2, summary.UpdatedCount);
        Assert.Equal(1, summary.SkippedCount);
        Assert.Equal(2, summary.FailedCount);
    }

    [Fact]
    public void BuildErrorReportDataUri_When_Error_Rows_Have_Special_Characters_Escapes_Csv_Content()
    {
        // Arrange
        var errorRows = new List<ImportErrorRowDto>
        {
            new(4, "First,Name", "Value \"quoted\"\nand wrapped", "bad@example.com"),
        };

        // Act
        var dataUri = ImportCustomersCsvProcessor.BuildErrorReportDataUri(errorRows);

        // Assert
        Assert.NotNull(dataUri);
        var csvPayload = Uri.UnescapeDataString(dataUri.Split(',', 2)[1]);
        Assert.Contains("LineNumber,Field,Message,Email", csvPayload, StringComparison.Ordinal);
        Assert.Contains("\"First,Name\"", csvPayload, StringComparison.Ordinal);
        Assert.Contains("\"Value \"\"quoted\"\"", csvPayload, StringComparison.Ordinal);
        Assert.Contains("bad@example.com", csvPayload, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMixedFieldSelections_When_Existing_Source_Is_Selected_Uses_Existing_Field_Value()
    {
        // Arrange
        var mappedCsv = Encoding.UTF8.GetBytes(AllCanonicalHeaders + "\n" + BuildCsvWithOverrides(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
        Assert.Equal(2, committedLines.Length);

        var committedHeaders = committedLines[0].Split(',');
        var committedValues = committedLines[1].Split(',');
        var headerIndexes = committedHeaders
            .Select((header, index) => new { header, index })
            .ToDictionary(item => item.header, item => item.index, StringComparer.Ordinal);

        Assert.Equal("IncomingFirst", committedValues[headerIndexes["FirstName"]]);
        Assert.Equal("ExistingLast", committedValues[headerIndexes["LastName"]]);
        Assert.Equal("mixed@example.com", committedValues[headerIndexes["Email"]]);
    }

    private static string BuildCsvWithOverrides(IReadOnlyDictionary<string, string> valuesByField)
    {
        var values = CustomerImportHeaderMatcher.Fields
            .Select(field => valuesByField.GetValueOrDefault(field.Name, "v"));

        return string.Join(",", values);
    }
}
