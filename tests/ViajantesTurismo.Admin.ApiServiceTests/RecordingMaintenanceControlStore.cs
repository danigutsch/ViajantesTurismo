using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Records validated maintenance controls and returns the selected store result.</summary>
internal sealed class RecordingMaintenanceControlStore(Result result) : IDocumentMaintenanceControlStore
{
    /// <summary>Gets the command accepted for mutation.</summary>
    public ControlDocumentMaintenanceCommand? Received
    {
        get;
        private set;
    }

    /// <summary>Gets the normalized reason stored in the audit record.</summary>
    public string? Reason
    {
        get;
        private set;
    }

    /// <inheritdoc />
    public ValueTask<Result> Apply(ControlDocumentMaintenanceCommand command, string reason, CancellationToken ct)
    {
        Received = command;
        Reason = reason;
        return ValueTask.FromResult(result);
    }
}
