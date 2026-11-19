namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Import;

/// <summary>Coordinates concurrent import confirmations and owns the independent component renderers.</summary>
internal sealed class ConcurrentImportCustomersBufferScenario : IDisposable
{
    private static readonly TimeSpan EventBudget = TimeSpan.FromSeconds(15);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<ImportCustomersBufferScenario> _circuits = [];

    /// <summary>Creates the requested number of prepared component instances.</summary>
    public ConcurrentImportCustomersBufferScenario(int circuitCount, bool remapHeaders)
    {
        try
        {
            for (var i = 0; i < circuitCount; i++)
            {
                _circuits.Add(new ImportCustomersBufferScenario(remapHeaders, _release.Task));
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Enters duplicate resolution after verifying all imports reached their upload boundary together.</summary>
    public async Task ConfirmConcurrently(CancellationToken ct)
    {
        var confirmations = _circuits.Select(circuit => circuit.ConfirmImport()).ToArray();
        await Task.WhenAll(_circuits.Select(circuit => circuit.UploadStarted)).WaitAsync(EventBudget, ct);
        _release.SetResult();
        await Task.WhenAll(confirmations).WaitAsync(EventBudget, ct);
    }

    /// <summary>Reports each renderer's exact retained CSV array payload size.</summary>
    public int[] RetainedCsvBytes() => [.. _circuits.Select(circuit => circuit.RetainedCsvBytes())];

    /// <summary>Gets whether all component instances reached duplicate resolution.</summary>
    public bool AwaitingConflictResolution => _circuits.All(circuit => circuit.AwaitingConflictResolution);

    /// <inheritdoc />
    public void Dispose()
    {
        _release.TrySetResult();
        foreach (var circuit in _circuits)
        {
            circuit.Dispose();
        }
    }
}
