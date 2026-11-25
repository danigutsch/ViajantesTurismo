using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Application.Documents.Maintenance;

/// <summary>Applies a validated scheduler change and its private audit record in the current transaction.</summary>
public interface IDocumentMaintenanceControlStore
{
    /// <summary>Applies the control once and recognizes an exact replay.</summary>
    ValueTask<Result> Apply(ControlDocumentMaintenanceCommand command, string reason, CancellationToken ct);
}
