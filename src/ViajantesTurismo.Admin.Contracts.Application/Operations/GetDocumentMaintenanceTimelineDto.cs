namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of counts-only results from one document maintenance worker.</summary>
public sealed record GetDocumentMaintenanceTimelineDto
{
    /// <summary>Gets the recorded results ordered newest first.</summary>
    public required IReadOnlyList<DocumentMaintenanceRunDto> Runs
    {
        get;
        init;
    }

    /// <summary>Gets the continuation for earlier results, or null at the end of the timeline.</summary>
    public DocumentMaintenanceRunCursorDto? Next
    {
        get;
        init;
    }

    /// <summary>Gets the durable schedule and execution observations for the selected job.</summary>
    public DocumentMaintenanceScheduleDto? Schedule
    {
        get;
        init;
    }
}
