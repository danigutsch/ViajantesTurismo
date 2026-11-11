using System.Globalization;
using System.Text;
using PandaBear.Tabular;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Provides CSV parsing and transformation helpers used by the customer import workflow.
/// </summary>
internal static class ImportCustomersCsvProcessor
{
    private static readonly CsvWriteOptions ImportFileOptions = new() { GuardFormulas = false, NewLine = "\n" };
    private static readonly string[] ErrorReportHeaders = ["LineNumber", "Field", "Message", "Email"];

    private sealed record MixedConflictRow(
        IReadOnlyList<string> Values,
        IReadOnlyDictionary<string, ImportConflictFieldSource> FieldSelections,
        IReadOnlyDictionary<string, string> ExistingValues);

    /// <summary>Reads the header names of a CSV file; when its rows are malformed, the first record alone is read.</summary>
    internal static IReadOnlyList<string> ReadHeaders(byte[] fileBytes)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        if (TryParse(fileBytes, out var document))
        {
            return document.Headers;
        }

        var headerEnd = Array.IndexOf(fileBytes, (byte)'\n');
        ReadOnlySpan<byte> firstRecord = headerEnd < 0 ? fileBytes : fileBytes.AsSpan(0, headerEnd);
        var header = CsvDocument.ParseUtf8(firstRecord);
        return header.IsSuccess ? header.Value.Headers : [];
    }

    /// <summary>Builds up to the given number of preview rows from the pending CSV using the effective column mappings.</summary>
    internal static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildPreviewRows(
        byte[] pendingFileBytes,
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
    internal static Dictionary<string, Dictionary<string, string>> ParseMappedRowsByEmail(byte[] mappedFileBytes)
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

    /// <summary>Rewrites the mapped CSV so rows with a mixed decision hold the per-field merged values.</summary>
    internal static byte[] ApplyMixedFieldSelections(
        byte[] mappedFileBytes,
        IReadOnlyList<ImportCustomerConflictState> conflictStates)
    {
        if (!TryParse(mappedFileBytes, out var document) || document.Rows.Count == 0)
        {
            return mappedFileBytes;
        }

        var headers = document.Headers;
        var emailIndex = FindHeader(headers, CustomerImportFieldNames.Email);
        if (emailIndex < 0)
        {
            return mappedFileBytes;
        }

        var headerIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            headerIndexes.TryAdd(headers[i], i);
        }

        var anyMerged = false;
        var rows = new List<IReadOnlyList<string>>(document.Rows.Count);
        foreach (var values in document.Rows)
        {
            if (!TryGetMixedConflictRow(values, emailIndex, conflictStates, out var conflictRow))
            {
                rows.Add(values);
                continue;
            }

            var merged = BuildMergedValues(headerIndexes, conflictRow);
            rows.Add(merged);
            anyMerged |= !merged.SequenceEqual(values, StringComparer.Ordinal);
        }

        return anyMerged ? WriteImportFile(headers, rows) : mappedFileBytes;
    }

    /// <summary>
    /// Writes a CSV file for the import API: values are kept exactly, without the spreadsheet formula guard, so the file reads back
    /// with the same headers and values.
    /// </summary>
    internal static byte[] WriteImportFile(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows) =>
        Encoding.UTF8.GetBytes(CsvWriter.Write(headers, rows, ImportFileOptions));

    /// <summary>Parses CSV bytes; a structurally invalid file yields no document.</summary>
    internal static bool TryParse(byte[] fileBytes, out CsvDocument document)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);

        var parsed = CsvDocument.ParseUtf8(fileBytes);
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

    private static bool TryGetMixedConflictRow(
        CsvRow values,
        int emailIndex,
        IReadOnlyList<ImportCustomerConflictState> conflictStates,
        out MixedConflictRow conflictRow)
    {
        conflictRow = null!;

        var email = values[emailIndex];
        var state = conflictStates.FirstOrDefault(candidate =>
            candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (state is null || !state.HasMixedDecision)
        {
            return false;
        }

        if (state.FieldSelections.Count == 0)
        {
            return false;
        }

        conflictRow = new MixedConflictRow(values, state.FieldSelections, state.ExistingValues);
        return true;
    }

    private static string[] BuildMergedValues(
        Dictionary<string, int> headerIndexes,
        MixedConflictRow conflictRow)
    {
        var mergedValues = conflictRow.Values.ToArray();

        foreach (var (fieldName, source) in conflictRow.FieldSelections)
        {
            if (source != ImportConflictFieldSource.Existing)
            {
                continue;
            }

            if (!headerIndexes.TryGetValue(fieldName, out var fieldIndex))
            {
                continue;
            }

            if (!conflictRow.ExistingValues.TryGetValue(fieldName, out var existingValue))
            {
                continue;
            }

            mergedValues[fieldIndex] = existingValue;
        }

        return mergedValues;
    }
}
