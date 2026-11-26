using ViajantesTurismo.Admin.Application.Documents.Maintenance;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Records one applied control without adding actor or reason to scheduler metadata.</summary>
/// <param name="RequestId">The stable command request identifier.</param>
/// <param name="Job">The maintenance job.</param>
/// <param name="Action">The applied control.</param>
/// <param name="FailedRunId">The optional failed execution targeted by a retry.</param>
/// <param name="ActorId">The authenticated operator subject.</param>
/// <param name="Reason">The operator-entered private reason.</param>
/// <param name="OccurredAtUtc">The time the control was applied.</param>
internal sealed record DocumentMaintenanceControlAudit(
    Guid RequestId,
    DocumentMaintenanceJob Job,
    DocumentMaintenanceControlAction Action,
    Guid? FailedRunId,
    string ActorId,
    string Reason,
    DateTimeOffset OccurredAtUtc)
{
    /// <summary>Checks whether the same actor replayed the exact accepted intent.</summary>
    internal bool Matches(ControlDocumentMaintenanceCommand command, string reason) =>
        Job == command.Job && Action == command.Action && FailedRunId == command.Input.FailedRunId
        && string.Equals(ActorId, command.ActorId, StringComparison.Ordinal)
        && string.Equals(Reason, reason, StringComparison.Ordinal);

    /// <inheritdoc />
    public override string ToString() => "DocumentMaintenanceControlAudit [redacted]";
}
