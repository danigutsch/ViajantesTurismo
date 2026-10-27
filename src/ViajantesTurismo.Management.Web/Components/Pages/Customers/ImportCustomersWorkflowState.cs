using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.HttpClients;
using ViajantesTurismo.Management.Web.Services;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers;

/// <summary>
/// Represents the mutable workflow state for the customer import page.
/// </summary>
internal sealed class ImportCustomersWorkflowState
{
    /// <summary>Counter of active drag-over events on the drop zone.</summary>
    internal int Dragging
    {
        get; set;
    }

    /// <summary>Validation message for the selected file, or null.</summary>
    internal string? ValidationError
    {
        get; set;
    }

    /// <summary>Current step of the import workflow.</summary>
    internal ImportCustomersWorkflowStep Step { get; set; } = ImportCustomersWorkflowStep.FileSelection;

    /// <summary>Name of the selected file.</summary>
    internal string PendingFileName { get; private set; } = string.Empty;

    /// <summary>Content of the selected file.</summary>
    internal byte[] PendingFileBytes { get; private set; } = [];

    /// <summary>Headers found in the selected CSV.</summary>
    internal IReadOnlyList<string> CsvHeaders { get; private set; } = [];

    /// <summary>Importable fields with their default column mappings.</summary>
    internal IReadOnlyList<CustomerImportFieldMapping> FieldMappings { get; private set; } = [];

    /// <summary>Column mapping overrides chosen by the user, keyed by field name.</summary>
    internal Dictionary<string, string?> UserMappings { get; private set; } = [];

    /// <summary>State of the in-progress upload.</summary>
    internal UploadOperationState Upload { get; } = new();

    /// <summary>Whether an upload is in progress.</summary>
    internal bool Uploading => Upload.IsActive;

    /// <summary>Sample rows shown before importing.</summary>
    internal IReadOnlyList<IReadOnlyDictionary<string, string>> PreviewRows { get; set; } = [];

    /// <summary>Error message from the last operation, or null.</summary>
    internal string? Error
    {
        get; set;
    }

    /// <summary>Outcome of the import, or null before it completes.</summary>
    internal ImportResultDto? Result
    {
        get; set;
    }

    /// <summary>Duplicate conflicts awaiting or holding a resolution.</summary>
    internal IReadOnlyList<ImportCustomerConflictState> ConflictStates { get; set; } = [];

    /// <summary>Mapped file content kept to resolve conflicts.</summary>
    internal byte[] ConflictSourceFileBytes { get; set; } = [];

    /// <summary>Stores the selected file and its mappings, clears prior results and moves to header mapping.</summary>
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
        ConflictSourceFileBytes = [];
        Result = null;
        Error = null;
        ValidationError = null;
        Step = ImportCustomersWorkflowStep.HeaderMapping;
    }

    /// <summary>Clears results, errors, conflicts, previews and upload state.</summary>
    internal void ClearTransientState()
    {
        Result = null;
        Error = null;
        ValidationError = null;
        ConflictStates = [];
        ConflictSourceFileBytes = [];
        PreviewRows = [];
        Upload.Reset();
    }

    /// <summary>Clears the selected file and its mappings.</summary>
    internal void ClearPendingFile()
    {
        PendingFileName = string.Empty;
        PendingFileBytes = [];
        CsvHeaders = [];
        FieldMappings = [];
        UserMappings = [];
    }

    /// <summary>Clears all state and returns to file selection.</summary>
    internal void ResetToFileSelection()
    {
        ClearTransientState();
        ClearPendingFile();
        Step = ImportCustomersWorkflowStep.FileSelection;
    }

    /// <summary>Clears transient state and returns to header mapping, or to file selection when no file is pending.</summary>
    internal void RetryCurrentFile()
    {
        ClearTransientState();
        Step = PendingFileBytes.Length > 0
            ? ImportCustomersWorkflowStep.HeaderMapping
            : ImportCustomersWorkflowStep.FileSelection;
    }

    /// <summary>Returns each conflict's decision keyed by email, with empty text when unresolved.</summary>
    internal Dictionary<string, string> BuildConflictDecisions() =>
        ConflictStates.ToDictionary(
            state => state.Email,
            state => state.Decision ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
}
