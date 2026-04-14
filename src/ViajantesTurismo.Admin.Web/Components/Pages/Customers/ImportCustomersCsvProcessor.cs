using System.Text;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

internal enum ImportConflictFieldSource
{
    Existing,
    Incoming,
}

internal sealed record ImportCustomersSummaryCounts(int CreatedCount, int UpdatedCount, int SkippedCount, int FailedCount);

internal static class ImportCustomersCsvProcessor
{
    private sealed record MixedConflictRow(
        string[] Values,
        IReadOnlyDictionary<string, ImportConflictFieldSource> FieldSelections,
        IReadOnlyDictionary<string, string> ExistingValues);

    internal static IReadOnlyList<IReadOnlyDictionary<string, string>> BuildPreviewRows(
        byte[] pendingFileBytes,
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<CustomerImportFieldMapping> fieldMappings,
        IReadOnlyDictionary<string, string?> userMappings,
        int maxPreviewRows)
    {
        var text = Encoding.UTF8.GetString(pendingFileBytes);
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return [];
        }

        var fieldToColIndex = BuildFieldToColumnIndex(csvHeaders, fieldMappings, userMappings);
        var result = new List<IReadOnlyDictionary<string, string>>();

        foreach (var line in lines.Skip(1).Take(maxPreviewRows))
        {
            var values = line.Split(',');
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (fieldName, colIdx) in fieldToColIndex)
            {
                row[fieldName] = colIdx < values.Length ? values[colIdx].Trim().Trim('"') : string.Empty;
            }

            result.Add(row);
        }

        return result.AsReadOnly();
    }

    internal static Dictionary<string, Dictionary<string, string>> ParseMappedRowsByEmail(byte[] mappedFileBytes)
    {
        var text = Encoding.UTF8.GetString(mappedFileBytes);
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var emailIndex = Array.FindIndex(headers, h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
        if (emailIndex < 0)
        {
            return new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var values = line.Split(',').Select(v => v.Trim().Trim('"')).ToArray();
            if (emailIndex >= values.Length)
            {
                continue;
            }

            var email = values[emailIndex];
            if (string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            var rowValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Length; i++)
            {
                rowValues[headers[i]] = i < values.Length ? values[i] : string.Empty;
            }

            result[email] = rowValues;
        }

        return result;
    }

    internal static byte[] ApplyMixedFieldSelections(
        byte[] mappedFileBytes,
        IReadOnlyDictionary<string, string> conflictDecisions,
        IReadOnlyDictionary<string, Dictionary<string, ImportConflictFieldSource>> mixedFieldSelectionsByEmail,
        IReadOnlyDictionary<string, Dictionary<string, string>> existingConflictValuesByEmail)
    {
        var text = Encoding.UTF8.GetString(mappedFileBytes);
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            return mappedFileBytes;
        }

        var headers = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToArray();
        var headerIndexes = headers
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        if (!headerIndexes.TryGetValue("Email", out var emailIndex))
        {
            return mappedFileBytes;
        }

        var outputLines = new List<string>(lines.Length) { lines[0] };
        foreach (var line in lines.Skip(1))
        {
            var values = line.Split(',').Select(v => v.Trim().Trim('"')).ToArray();
            if (!TryGetMixedConflictRow(values, emailIndex, conflictDecisions, mixedFieldSelectionsByEmail, existingConflictValuesByEmail, out var conflictRow))
            {
                outputLines.Add(line);
                continue;
            }

            var mergedValues = BuildMergedValues(headers, headerIndexes, conflictRow);
            outputLines.Add(string.Join(",", mergedValues.Select(EscapeCsvValue)));
        }

        return Encoding.UTF8.GetBytes(string.Join("\n", outputLines));
    }

    internal static ImportCustomersSummaryCounts BuildImportSummary(
        ImportResultDto? result,
        IReadOnlyDictionary<string, string> conflictDecisions)
    {
        if (result is null)
        {
            return new ImportCustomersSummaryCounts(0, 0, 0, 0);
        }

        var skippedCount = conflictDecisions.Values.Count(v =>
            v.Equals("keep", StringComparison.OrdinalIgnoreCase));
        var updatedCount = conflictDecisions.Values.Count(v =>
            v.Equals("overwrite", StringComparison.OrdinalIgnoreCase)
            || v.Equals("mixed", StringComparison.OrdinalIgnoreCase));
        var createdCount = Math.Max(0, result.SuccessCount - updatedCount);

        return new ImportCustomersSummaryCounts(
            createdCount,
            updatedCount,
            skippedCount,
            result.ErrorCount);
    }

    internal static string? BuildErrorReportDataUri(IReadOnlyList<ImportErrorRowDto> errorRows)
    {
        if (errorRows.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine("LineNumber,Field,Message,Email");
        foreach (var row in errorRows)
        {
            sb.Append(row.LineNumber)
                .Append(',')
                .Append(EscapeCsvValue(row.Field ?? string.Empty))
                .Append(',')
                .Append(EscapeCsvValue(row.Message))
                .Append(',')
                .Append(EscapeCsvValue(row.Email ?? string.Empty))
                .AppendLine();
        }

        return "data:text/csv;charset=utf-8," + Uri.EscapeDataString(sb.ToString());
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
        string[] values,
        int emailIndex,
        IReadOnlyDictionary<string, string> conflictDecisions,
        IReadOnlyDictionary<string, Dictionary<string, ImportConflictFieldSource>> mixedFieldSelectionsByEmail,
        IReadOnlyDictionary<string, Dictionary<string, string>> existingConflictValuesByEmail,
        out MixedConflictRow conflictRow)
    {
        conflictRow = null!;

        if (emailIndex >= values.Length)
        {
            return false;
        }

        var email = values[emailIndex];
        if (!conflictDecisions.TryGetValue(email, out var decision)
            || !decision.Equals("mixed", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!mixedFieldSelectionsByEmail.TryGetValue(email, out var fieldSelections)
            || !existingConflictValuesByEmail.TryGetValue(email, out var existingValues))
        {
            return false;
        }

        conflictRow = new MixedConflictRow(values, fieldSelections, existingValues);
        return true;
    }

    private static string[] BuildMergedValues(
        IReadOnlyList<string> headers,
        Dictionary<string, int> headerIndexes,
        MixedConflictRow conflictRow)
    {
        var mergedValues = headers.Select((_, i) => i < conflictRow.Values.Length ? conflictRow.Values[i] : string.Empty).ToArray();

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

    private static string EscapeCsvValue(string value) =>
        value.Contains(',', StringComparison.Ordinal)
        || value.Contains('"', StringComparison.Ordinal)
        || value.Contains('\n', StringComparison.Ordinal)
        || value.Contains('\r', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
}
