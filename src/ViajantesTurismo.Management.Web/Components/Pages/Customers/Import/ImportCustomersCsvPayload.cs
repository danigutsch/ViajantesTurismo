namespace ViajantesTurismo.Management.Web.Components.Pages.Customers.Import;

/// <summary>Describes the headers and per-row overrides in the last request without holding that request's bytes.</summary>
internal sealed class ImportCustomersCsvPayload
{
    private static readonly Dictionary<int, Dictionary<int, string>> NoOverrides = [];
    private readonly IReadOnlyList<string>? _mappedHeaders;
    private readonly Dictionary<int, Dictionary<int, string>> _appliedOverrides;

    /// <summary>Captures the remapped headers, or uses the original headers when there are no changes.</summary>
    internal ImportCustomersCsvPayload(IReadOnlyList<string>? mappedHeaders)
        : this(mappedHeaders, NoOverrides)
    {
    }

    private ImportCustomersCsvPayload(
        IReadOnlyList<string>? mappedHeaders,
        Dictionary<int, Dictionary<int, string>> appliedOverrides)
    {
        _mappedHeaders = mappedHeaders;
        _appliedOverrides = appliedOverrides;
    }

    /// <summary>Builds the upload and records only its effective changes for a potential conflict retry.</summary>
    internal (ReadOnlyMemory<byte> FileContent, ImportCustomersCsvPayload Payload) Prepare(
        ReadOnlyMemory<byte> selectedFile,
        IReadOnlyList<ImportCustomerConflictState> conflicts)
    {
        var (fileContent, newOverrides) = ImportCustomersCsvProcessor.RewriteImportFile(selectedFile, _mappedHeaders, _appliedOverrides, conflicts);
        return (fileContent, newOverrides is null ? this : new ImportCustomersCsvPayload(_mappedHeaders, newOverrides));
    }
}
