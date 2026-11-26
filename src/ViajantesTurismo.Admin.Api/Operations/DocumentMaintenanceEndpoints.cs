using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.AspNetCore.Authentication;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Maintenance;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api.Operations;

/// <summary>Maps document maintenance history and separately authorized controls.</summary>
internal static class DocumentMaintenanceEndpoints
{
    /// <summary>Registers maintenance queries and confirmed controls.</summary>
    /// <param name="app">The Admin API under configuration.</param>
    public static void MapDocumentMaintenanceEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var operations = app.MapOperationsGroup(AdminAuthorization.OperationsScheduleRead);
        operations.MapGet("/document-maintenance", Get)
            .WithAdminMetadata("GetDocumentMaintenanceHistory",
                "Reads recent counts-only document maintenance batches and the current durable schedule metadata.",
                "Gets document maintenance history and schedules.");
        operations.MapGet("/document-maintenance/{job}", GetTimeline)
            .WithAdminMetadata("GetDocumentMaintenanceTimeline",
                "Reads a bounded page of counts-only batches and current durable schedule metadata for one job.",
                "Gets document maintenance timeline.");
        operations.MapGet("/document-maintenance/{job}/executions", GetExecutions)
            .WithAdminMetadata("GetDocumentMaintenanceExecutions", "Reads a bounded page of durable execution metadata for one maintenance job.",
                "Gets document maintenance executions.");
        operations.MapPost("/document-maintenance/{job}/{action}", Control)
            .RequireAuthorization(AdminAuthorization.OperationsScheduleControl)
            .RequireRateLimiting(AdminSecurityBaseline.MutationRateLimitPolicy)
            .Produces(StatusCodes.Status429TooManyRequests)
            .WithAdminMetadata("ControlDocumentMaintenance", "Applies a confirmed pause, resume, trigger or retry for an allowlisted maintenance job.",
                "Controls a document maintenance schedule.");
    }

    private static async Task<Ok<DocumentMaintenanceHistoryDto>> Get(
        [FromServices] IDocumentMaintenanceHistoryQuery query,
        CancellationToken ct)
    {
        var history = await query.Read(ct);
        return TypedResults.Ok(history);
    }

    private static async Task<Results<Ok<GetDocumentMaintenanceTimelineDto>, BadRequest, NotFound>> GetTimeline(
        [FromServices] IDocumentMaintenanceTimelineQuery query, string job, DateTimeOffset? beforeOccurredAt,
        Guid? beforeRunId, int? limit, CancellationToken ct)
    {
        if (!TryReadJob(job, out var selected))
        {
            return TypedResults.NotFound();
        }

        var pageSize = limit ?? ContractConstants.DefaultDocumentMaintenancePageSize;
        if (InvalidPage(pageSize, beforeOccurredAt, beforeRunId))
        {
            return TypedResults.BadRequest();
        }

        var before = beforeOccurredAt is { } time && beforeRunId is { } runId
            ? new DocumentMaintenanceRunCursorDto(time, runId)
            : null;
        return TypedResults.Ok(await query.Read(selected, pageSize, before, ct));
    }

    private static async Task<Results<Ok<GetDocumentMaintenanceExecutionsDto>, BadRequest, NotFound>> GetExecutions(
        [FromServices] IDocumentMaintenanceExecutionsQuery query, string job, DateTimeOffset? beforeStartedAt,
        Guid? beforeRunId, int? limit, CancellationToken ct)
    {
        if (!TryReadJob(job, out var selected))
        {
            return TypedResults.NotFound();
        }

        var pageSize = limit ?? ContractConstants.DefaultDocumentMaintenancePageSize;
        if (InvalidPage(pageSize, beforeStartedAt, beforeRunId))
        {
            return TypedResults.BadRequest();
        }

        var before = beforeStartedAt is { } time && beforeRunId is { } runId
            ? new DocumentMaintenanceExecutionCursorDto(time, runId)
            : null;
        return TypedResults.Ok(await query.ReadExecutions(selected, pageSize, before, ct));
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound, ValidationProblem, Conflict<ProblemDetails>>> Control(
        string job, string action, [FromBody] CreateDocumentMaintenanceControlDto input, [FromServices] ISender sender,
        HttpContext context, CancellationToken ct)
    {
        if (!TryReadJob(job, out var selectedJob)
            || !Enum.TryParse<DocumentMaintenanceControlAction>(action, ignoreCase: true, out var selectedAction)
            || !string.Equals(selectedAction.ToString(), action, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }

        var actorId = context.User.GetSubjectId();
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await sender.Send(new ControlDocumentMaintenanceCommand(selectedJob, selectedAction, input, actorId), ct);
        return result.Status switch
        {
            ResultStatus.Ok => TypedResults.NoContent(),
            ResultStatus.NotFound => TypedResults.NotFound(),
            ResultStatus.Invalid => result.ToValidationProblem(),
            ResultStatus.Conflict => result.ToConflict(),
            _ => throw new InvalidOperationException("Unexpected maintenance control result.")
        };
    }

    private static bool TryReadJob(string job, out DocumentMaintenanceJob selected) =>
        Enum.TryParse(job, ignoreCase: true, out selected)
        && string.Equals(selected.ToString(), job, StringComparison.OrdinalIgnoreCase);

    private static bool InvalidPage(int pageSize, DateTimeOffset? beforeTime, Guid? beforeRunId) =>
        pageSize is < 1 or > ContractConstants.MaxDocumentMaintenancePageSize
        || beforeTime.HasValue != beforeRunId.HasValue || beforeRunId == Guid.Empty;
}
