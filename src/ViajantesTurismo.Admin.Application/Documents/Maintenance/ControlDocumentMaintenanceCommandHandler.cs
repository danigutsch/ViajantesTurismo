using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents.Maintenance;

/// <summary>Validates operator intent and applies a maintenance control through the Admin transaction.</summary>
public sealed class ControlDocumentMaintenanceCommandHandler(
    IDocumentMaintenanceControlStore store) : ICommandHandler<ControlDocumentMaintenanceCommand, Result>
{
    /// <summary>Applies a confirmed, bounded control for an existing maintenance job.</summary>
    public ValueTask<Result> Handle(ControlDocumentMaintenanceCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!Enum.IsDefined(command.Job) || !Enum.IsDefined(command.Action))
        {
            return ValueTask.FromResult(Result.NotFound(detail: "The maintenance control was not found."));
        }

        var reason = command.Input.Reason?.Trim();
        if (!command.Input.Confirmed || command.Input.RequestId == Guid.Empty
            || string.IsNullOrEmpty(reason) || reason.Length > ContractConstants.MaxDocumentMaintenanceControlReasonLength
            || reason.Contains('\0', StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(command.ActorId) || command.ActorId.Length > DocumentAuditLimits.MaxActorIdLength
            || !HasValidTarget(command))
        {
            return ValueTask.FromResult(Result.Invalid(
                detail: "The maintenance control requires a valid request ID, confirmation, reason and target.",
                field: "maintenanceControl",
                message: "The maintenance control is not valid."));
        }

        return store.Apply(command, reason, ct);
    }

    private static bool HasValidTarget(ControlDocumentMaintenanceCommand command) =>
        command.Action == DocumentMaintenanceControlAction.Retry
            ? command.Input.FailedRunId is { } runId && runId != Guid.Empty
            : command.Input.FailedRunId is null;
}
