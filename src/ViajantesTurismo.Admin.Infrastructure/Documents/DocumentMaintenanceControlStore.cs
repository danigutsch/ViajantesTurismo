using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SharedKernel.Npgsql;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Applies the scheduler mutation and appends its private audit using the Admin command transaction.</summary>
internal sealed class DocumentMaintenanceControlStore(
    AdminWriteDbContext database,
    PostgreSqlJobScheduler scheduler,
    TimeProvider timeProvider) : IDocumentMaintenanceControlStore
{
    /// <inheritdoc />
    public async ValueTask<Result> Apply(ControlDocumentMaintenanceCommand command, string reason, CancellationToken ct)
    {
        if (database.Database.CurrentTransaction?.GetDbTransaction() is not NpgsqlTransaction transaction)
        {
            throw new InvalidOperationException("Schedule controls require the Admin PostgreSQL transaction.");
        }

        var audit = database.Set<DocumentMaintenanceControlAudit>();
        var existing = await audit.AsNoTracking()
            .SingleOrDefaultAsync(record => record.RequestId == command.Input.RequestId, ct);
        if (existing is not null)
        {
            return existing.Matches(command, reason)
                ? Result.Ok()
                : Result.Conflict(detail: "This maintenance request ID is already in use.");
        }

        var jobName = DocumentMaintenanceSchedule.GetJobName(command.Job);
        var accepted = command.Action switch
        {
            DocumentMaintenanceControlAction.Pause => await scheduler.SetPaused(jobName, true, transaction, ct),
            DocumentMaintenanceControlAction.Resume => await scheduler.SetPaused(jobName, false, transaction, ct),
            DocumentMaintenanceControlAction.Trigger => await scheduler.Trigger(jobName, command.Input.RequestId, transaction, ct),
            DocumentMaintenanceControlAction.Retry => await scheduler.Retry(
                jobName, command.Input.FailedRunId!.Value, command.Input.RequestId, transaction, ct),
            _ => throw new InvalidOperationException("The maintenance action is unsupported."),
        };

        if (!accepted)
        {
            return Result.Conflict(detail: "The maintenance schedule or execution cannot accept this action.");
        }

        audit.Add(new DocumentMaintenanceControlAudit(
            command.Input.RequestId, command.Job, command.Action, command.Input.FailedRunId,
            command.ActorId, reason, timeProvider.GetUtcNow()));
        await database.SaveEntities(ct);
        return Result.Ok();
    }
}
