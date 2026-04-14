using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.Web.Services;

namespace ViajantesTurismo.Admin.Web.Components.Pages.Customers;

/// <summary>
/// Represents the mutable workflow state for the customer import page.
/// </summary>
internal sealed class ImportCustomersWorkflowState
{
    internal int Dragging { get; set; }

    internal string? ValidationError { get; set; }

    internal ImportCustomersWorkflowStep Step { get; set; } = ImportCustomersWorkflowStep.FileSelection;

    internal string PendingFileName { get; private set; } = string.Empty;

    internal byte[] PendingFileBytes { get; private set; } = [];

    internal IReadOnlyList<string> CsvHeaders { get; private set; } = [];

    internal IReadOnlyList<CustomerImportFieldMapping> FieldMappings { get; private set; } = [];

    internal Dictionary<string, string?> UserMappings { get; private set; } = [];

    internal bool Uploading { get; set; }

    internal IReadOnlyList<IReadOnlyDictionary<string, string>> PreviewRows { get; set; } = [];

    internal string? Error { get; set; }

    internal ImportResultDto? Result { get; set; }

    internal IReadOnlyList<ImportCustomerConflictState> ConflictStates { get; set; } = [];

    internal void SetPendingFile(
        string fileName,
        byte[] fileBytes,
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<CustomerImportFieldMapping> fieldMappings)
    {
        PendingFileName = fileName;
        PendingFileBytes = fileBytes;
        CsvHeaders = csvHeaders;
        FieldMappings = fieldMappings;
        UserMappings = [];
        PreviewRows = [];
        ConflictStates = [];
        Result = null;
        Error = null;
        ValidationError = null;
        Step = ImportCustomersWorkflowStep.HeaderMapping;
    }

    internal void ClearTransientState()
    {
        Result = null;
        Error = null;
        ValidationError = null;
        ConflictStates = [];
        PreviewRows = [];
        Uploading = false;
    }

    internal void ClearPendingFile()
    {
        PendingFileName = string.Empty;
        PendingFileBytes = [];
        CsvHeaders = [];
        FieldMappings = [];
        UserMappings = [];
    }

    internal void ResetToFileSelection()
    {
        ClearTransientState();
        ClearPendingFile();
        Step = ImportCustomersWorkflowStep.FileSelection;
    }

    internal void RetryCurrentFile()
    {
        ClearTransientState();
        Step = PendingFileBytes.Length > 0
            ? ImportCustomersWorkflowStep.HeaderMapping
            : ImportCustomersWorkflowStep.FileSelection;
    }

    internal Dictionary<string, string> BuildConflictDecisions() =>
        ConflictStates.ToDictionary(
            state => state.Email,
            state => state.Decision ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
}
