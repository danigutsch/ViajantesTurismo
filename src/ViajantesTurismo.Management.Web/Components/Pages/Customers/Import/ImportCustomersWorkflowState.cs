using ViajantesTurismo.Admin.Contracts.Application;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>
/// Represents the mutable workflow state for the customer import page.
/// </summary>
internal sealed class ImportCustomersWorkflowState
{
    private static readonly IReadOnlyDictionary<string, List<CustomerImportColumnSuggestion>> NoHeaderSuggestions =
        new Dictionary<string, List<CustomerImportColumnSuggestion>>(StringComparer.Ordinal);

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
    internal ReadOnlyMemory<byte> PendingFileBytes
    {
        get; private set;
    }

    /// <summary>Headers found in the selected CSV.</summary>
    internal IReadOnlyList<string> CsvHeaders { get; private set; } = [];

    /// <summary>Importable fields with their default column mappings.</summary>
    internal IReadOnlyList<CustomerImportFieldMapping> FieldMappings { get; private set; } = [];

    /// <summary>Suggested columns for fields that need a mapping choice.</summary>
    internal IReadOnlyDictionary<string, List<CustomerImportColumnSuggestion>> HeaderSuggestions
    {
        get; private set;
    } = NoHeaderSuggestions;

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

    /// <summary>Changes applied to the original file in the request that returned these conflicts.</summary>
    internal ImportCustomersCsvPayload? ConflictPayload
    {
        get; set;
    }

    /// <summary>Stores the selected file and its mappings, clears prior results and moves to header mapping.</summary>
    internal void SetPendingFile(
        string fileName,
        ReadOnlyMemory<byte> fileBytes,
        IReadOnlyList<string> csvHeaders,
        IReadOnlyList<CustomerImportFieldMapping> fieldMappings,
        IReadOnlyDictionary<string, List<CustomerImportColumnSuggestion>>? headerSuggestions = null)
    {
        PendingFileName = fileName;
        PendingFileBytes = fileBytes;
        CsvHeaders = csvHeaders;
        FieldMappings = fieldMappings;
        HeaderSuggestions = headerSuggestions ?? NoHeaderSuggestions;
        UserMappings = [];
        PreviewRows = [];
        ConflictStates = [];
        ConflictPayload = null;
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
        ConflictPayload = null;
        PreviewRows = [];
        Upload.Reset();
    }

    /// <summary>Clears the selected file and its mappings.</summary>
    internal void ClearPendingFile()
    {
        PendingFileName = string.Empty;
        PendingFileBytes = ReadOnlyMemory<byte>.Empty;
        CsvHeaders = [];
        FieldMappings = [];
        HeaderSuggestions = NoHeaderSuggestions;
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
