using System.Globalization;
using System.Text;
using SharedKernel.Results;
using SharedKernel.Tabular;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>
/// Provides CSV parsing and transformation helpers used by the customer import workflow.
/// </summary>
internal static class ImportCustomersCsvProcessor
{
    private static readonly CsvWriteOptions ImportFileOptions = new() { GuardFormulas = false, NewLine = "\n" };
    private static readonly string[] ErrorReportHeaders = ["LineNumber", "Field", "Message", "Email"];

    /// <summary>Reads a selected CSV and checks whether its columns can be mapped by name.</summary>
    internal static Result<CsvDocument> ReadForMapping(ReadOnlyMemory<byte> fileBytes)
    {
        var parsed = CsvDocument.ParseUtf8(fileBytes.Span);
        if (!parsed.IsSuccess)
        {
            return parsed;
        }

        var document = parsed.Value;
        if (document.Rows.Count == 0)
        {
            return Result.Invalid<CsvDocument>("The CSV needs at least one data row after the headers.", "rows", "No data rows.");
        }

        if (document.Headers.Any(string.IsNullOrWhiteSpace))
        {
            return Result.Invalid<CsvDocument>("Each column needs a name before it can be mapped.", "headers", "Unnamed column.");
        }

        if (document.Headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != document.Headers.Count)
        {
            return Result.Invalid<CsvDocument>(
                "Column names must be unique. Rename repeated columns and choose the file again.",
                "headers",
                "Repeated column name.");
        }

        return parsed;
    }

    /// <summary>Reads the header names of a CSV file; when its rows are malformed, the first record alone is read.</summary>
    internal static IReadOnlyList<string> ReadHeaders(ReadOnlyMemory<byte> fileBytes)
    {
        if (TryParse(fileBytes, out var document))
        {
            return document.Headers;
        }

        var bytes = fileBytes.Span;
        var headerEnd = bytes.IndexOf((byte)'\n');
        var firstRecord = headerEnd < 0 ? bytes : bytes[..headerEnd];
        var header = CsvDocument.ParseUtf8(firstRecord);
        return header.IsSuccess ? header.Value.Headers : [];
    }

    /// <summary>Builds up to the given number of preview rows from the pending CSV using the effective column mappings.</summary>
    internal static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildPreviewRows(
        ReadOnlyMemory<byte> pendingFileBytes,
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<CustomerImportFieldMapping> fieldMappings,
        IReadOnlyDictionary<string, string?> userMappings,
        int maxPreviewRows)
    {
        if (!TryParse(pendingFileBytes, out var document) || document.Rows.Count == 0)
        {
            return [];
        }

        var fieldToColIndex = BuildFieldToColumnIndex(csvHeaders, fieldMappings, userMappings);
        var result = new List<IReadOnlyDictionary<string, string>>();

        foreach (var values in document.Rows.Take(maxPreviewRows))
        {
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (fieldName, colIdx) in fieldToColIndex)
            {
                row[fieldName] = colIdx < values.Count ? values[colIdx] : string.Empty;
            }

            result.Add(row);
        }

        return result.AsReadOnly();
    }

    /// <summary>Parses the mapped CSV into field values keyed by email address.</summary>
    internal static Dictionary<string, Dictionary<string, string>> ParseMappedRowsByEmail(ReadOnlyMemory<byte> mappedFileBytes)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        if (!TryParse(mappedFileBytes, out var document))
        {
            return result;
        }

        var headers = document.Headers;
        var emailIndex = FindHeader(headers, CustomerImportFieldNames.Email);
        if (emailIndex < 0)
        {
            return result;
        }

        foreach (var values in document.Rows)
        {
            var email = values[emailIndex];
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                rowValues[headers[i]] = values[i];
            }

            result[email] = rowValues;
        }

        return result;
    }

    /// <summary>Applies header mappings and mixed decisions in one render, returning only new or changed field values.</summary>
    internal static (ReadOnlyMemory<byte> FileContent, Dictionary<int, Dictionary<int, string>>? NewOverrides) RewriteImportFile(
        ReadOnlyMemory<byte> fileBytes,
        IReadOnlyList<string>? mappedHeaders,
        Dictionary<int, Dictionary<int, string>> appliedOverrides,
        IReadOnlyList<ImportCustomerConflictState> conflicts)
    {
        var mixed = conflicts.Where(conflict => conflict.HasMixedDecision).ToDictionary(conflict => conflict.Email, StringComparer.OrdinalIgnoreCase);
        if ((mappedHeaders is null && appliedOverrides.Count == 0 && mixed.Count == 0) || !TryParse(fileBytes, out var document))
        {
            return (fileBytes, null);
        }

        var headers = mappedHeaders ?? document.Headers;
        var emailIndex = FindHeader(headers, CustomerImportFieldNames.Email);
        if (emailIndex < 0 || (appliedOverrides.Count == 0 && mixed.Count == 0))
        {
            return (mappedHeaders is null ? fileBytes : WriteImportFile(headers, document.Rows), null);
        }

        var headerIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            headerIndexes.TryAdd(headers[i], i);
        }

        Dictionary<int, Dictionary<int, string>>? newOverrides = null;
        var rows = new List<IReadOnlyList<string>>(document.Rows.Count);
        for (var i = 0; i < document.Rows.Count; i++)
        {
            var values = document.Rows[i];
            var previous = appliedOverrides.GetValueOrDefault(i);
            var currentEmail = previous?.GetValueOrDefault(emailIndex) ?? values[emailIndex];
            var added = GetNewOverrides(values, headerIndexes, previous, mixed.GetValueOrDefault(currentEmail));
            if (added is not null)
            {
                newOverrides ??= new Dictionary<int, Dictionary<int, string>>(appliedOverrides);
                newOverrides[i] = added;
            }

            rows.Add(ApplyOverrides(values, added ?? previous));
        }

        var changed = mappedHeaders is not null || appliedOverrides.Count > 0 || newOverrides is not null;
        return (changed ? WriteImportFile(headers, rows) : fileBytes, newOverrides);
    }

    /// <summary>
    /// Writes a CSV file for the import API: values are kept exactly, without the spreadsheet formula guard, so the file reads back
    /// with the same headers and values.
    /// </summary>
    internal static ReadOnlyMemory<byte> WriteImportFile(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows) =>
        Encoding.UTF8.GetBytes(CsvWriter.Write(headers, rows, ImportFileOptions));

    /// <summary>Parses CSV bytes; a structurally invalid file yields no document.</summary>
    internal static bool TryParse(ReadOnlyMemory<byte> fileBytes, out CsvDocument document)
    {
        var parsed = CsvDocument.ParseUtf8(fileBytes.Span);
        document = parsed.IsSuccess ? parsed.Value : null!;
        return parsed.IsSuccess;
    }

    /// <summary>Builds created, updated, skipped and failed counts from the import result and conflict decisions.</summary>
    internal static ImportCustomersSummaryCounts BuildImportSummary(
        ImportResultDto? result,
        IReadOnlyList<ImportCustomerConflictState> conflictStates)
    {
        if (result is null)
        {
            return new ImportCustomersSummaryCounts(0, 0, 0, 0);
        }

        var skippedCount = conflictStates.Count(state =>
            string.Equals(state.Decision, "keep", StringComparison.OrdinalIgnoreCase));
        var updatedCount = conflictStates.Count(state =>
            string.Equals(state.Decision, "overwrite", StringComparison.OrdinalIgnoreCase)
            || string.Equals(state.Decision, "mixed", StringComparison.OrdinalIgnoreCase));
        var createdCount = Math.Max(0, result.SuccessCount - updatedCount);

        return new ImportCustomersSummaryCounts(
            createdCount,
            updatedCount,
            skippedCount,
            result.ErrorCount);
    }

    /// <summary>Builds a CSV data URI of the error rows, or null when there are none.</summary>
    internal static string? BuildErrorReportDataUri(IReadOnlyList<ImportErrorRowDto> errorRows)
    {
        if (errorRows.Count == 0)
        {
            return null;
        }

        var csv = CsvWriter.Write(
            ErrorReportHeaders,
            errorRows.Select(row => (IReadOnlyList<string>)
            [
                row.LineNumber.ToString(CultureInfo.InvariantCulture),
                row.Field ?? string.Empty,
                row.Message,
                row.Email ?? string.Empty,
            ]));

        return "data:text/csv;charset=utf-8," + Uri.EscapeDataString(csv);
    }

    private static int FindHeader(IReadOnlyList<string> headers, string name)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildFieldToColumnIndex(
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<CustomerImportFieldMapping> fieldMappings,
        IReadOnlyDictionary<string, string?> userMappings)
    {
        var fieldToColIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var mapping in fieldMappings)
        {
            var csvCol = GetEffectiveMappingValue(mapping.Field.Name, mapping.MatchedCsvHeader, userMappings);
            if (csvCol is null)
            {
                continue;
            }

            for (var i = 0; i < csvHeaders.Count; i++)
            {
                if (string.Equals(csvHeaders[i], csvCol, StringComparison.OrdinalIgnoreCase))
                {
                    fieldToColIndex[mapping.Field.Name] = i;
                    break;
                }
            }
        }

        return fieldToColIndex;
    }

    private static string? GetEffectiveMappingValue(
        string fieldName,
        string? autoMatchedHeader,
        IReadOnlyDictionary<string, string?> userMappings)
    {
        return userMappings.GetValueOrDefault(fieldName, autoMatchedHeader);
    }

    private static Dictionary<int, string>? GetNewOverrides(
        CsvRow original,
        Dictionary<string, int> headerIndexes,
        Dictionary<int, string>? previous,
        ImportCustomerConflictState? conflict)
    {
        if (conflict is null)
        {
            return null;
        }

        Dictionary<int, string>? replacements = null;
        foreach (var (field, choice) in conflict.FieldSelections)
        {
            if (choice == ImportConflictFieldSource.Existing
                && headerIndexes.TryGetValue(field, out var index)
                && conflict.ExistingValues.TryGetValue(field, out var existingValue)
                && !string.Equals(previous?.GetValueOrDefault(index) ?? original[index], existingValue, StringComparison.Ordinal))
            {
                replacements ??= previous is null ? [] : new Dictionary<int, string>(previous);
                replacements[index] = existingValue;
            }
        }

        return replacements;
    }

    private static IReadOnlyList<string> ApplyOverrides(CsvRow original, Dictionary<int, string>? replacements)
    {
        if (replacements is null)
        {
            return original;
        }

        var values = original.ToArray();
        foreach (var (index, value) in replacements)
        {
            values[index] = value;
        }

        return values;
    }
}
