using System.Reflection;
using System.Runtime.InteropServices;
using PandaBear.Blazor;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

/// <summary>Drives a real import component while coordinating the fake HTTP response across independent renderers.</summary>
internal sealed class ImportCustomersBufferScenario : IDisposable
{
    private const string ConflictEmail = "existing@example.test";
    private readonly BunitContext _context = new();
    private readonly TaskCompletionSource _uploadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IRenderedComponent<ImportCustomers> _page;

    /// <summary>Creates a prepared component using canonical or upper-case headers and a one-MiB CSV.</summary>
    public ImportCustomersBufferScenario(bool remapHeaders, Task releaseUpload)
    {
        try
        {
            var customers = new FakeCustomersApiClient();
            customers.SetImportCustomersHandler(async (_, _, _, ct) =>
            {
                _uploadStarted.TrySetResult();
                await releaseUpload.WaitAsync(ct);
                return new ImportResultDto(0, 0, [new ImportConflictDto(ConflictEmail)]);
            });
            _context.Services.AddUiFailureReporting();
            _context.JSInterop.Mode = JSRuntimeMode.Loose;
            _context.Services.AddSingleton<ICustomersApiClient>(customers);
            _page = ImportCustomersPreviewTestHelper.GoToPreview(_context, CreateCsv(remapHeaders));
        }
        catch
        {
            _context.Dispose();
            throw;
        }
    }

    /// <summary>Completes once the component has passed its upload buffer to the HTTP client.</summary>
    public Task UploadStarted => _uploadStarted.Task;

    /// <summary>Dispatches the confirmation event from the preview screen.</summary>
    public Task ConfirmImport() => ImportCustomersTestDomHelper.FindButtonByText(_page, "Confirm Import").ClickAsync();

    /// <summary>Counts the distinct managed byte arrays rooted by the component's live workflow state.</summary>
    public int RetainedCsvBytes()
    {
        var state = typeof(ImportCustomers).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(_page.Instance)
            ?? throw new InvalidOperationException("The import component has no workflow state to measure.");
        var buffers = new HashSet<byte[]>(ReferenceEqualityComparer.Instance);
        foreach (var property in state.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            var buffer = property.GetValue(state) switch
            {
                byte[] bytes => bytes,
                ReadOnlyMemory<byte> memory when MemoryMarshal.TryGetArray(memory, out var segment) => segment.Array,
                _ => null
            };
            if (buffer is { Length: > 0 })
            {
                buffers.Add(buffer);
            }
        }
        return buffers.Sum(buffer => buffer.Length);
    }

    /// <summary>Confirms the renderer reached the step where the circuit retains bytes while awaiting an answer.</summary>
    public bool AwaitingConflictResolution => _page.Markup.Contains("Resolve Duplicates", StringComparison.Ordinal);

    /// <inheritdoc />
    public void Dispose()
    {
        _page.Dispose();
        _context.Dispose();
    }

    private static string CreateCsv(bool remapHeaders)
    {
        var source = CustomerImportCsvTestData.BuildCsvWithEmail(ConflictEmail);
        var headerEnd = source.IndexOf('\n', StringComparison.Ordinal);
        var headers = source[..headerEnd];
        if (remapHeaders)
        {
            headers = headers.ToUpperInvariant();
        }
        var prefix = headers + ",Ignored" + source[headerEnd..] + ",";
        return prefix + new string('x', (int)AdminContractConstants.CustomerImportMaxFileBytes - prefix.Length - 1) + "\n";
    }
}
