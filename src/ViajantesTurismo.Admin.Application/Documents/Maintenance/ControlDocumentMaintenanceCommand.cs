using SharedKernel.Mediator;
using SharedKernel.Mediator.Transactions;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Documents.Maintenance;

/// <summary>Applies one confirmed and audited maintenance control.</summary>
/// <param name="Job">The selected maintenance job.</param>
/// <param name="Action">The requested control.</param>
/// <param name="Input">The caller's confirmation, operation ID, reason and optional failed execution.</param>
/// <param name="ActorId">The subject from the authenticated principal.</param>
public sealed record ControlDocumentMaintenanceCommand(
    DocumentMaintenanceJob Job,
    DocumentMaintenanceControlAction Action,
    CreateDocumentMaintenanceControlDto Input,
    string ActorId) : ICommand<Result>, ITransactionLockDeclaration
{
    private const string RequestLockPrefix = "admin:document-maintenance-control:";

    IReadOnlyList<string> ITransactionLockDeclaration.TransactionLocks =>
        [string.Concat(RequestLockPrefix, Input.RequestId.ToString("N"))];

    /// <inheritdoc />
    public override string ToString() => "ControlDocumentMaintenanceCommand [redacted]";
}
