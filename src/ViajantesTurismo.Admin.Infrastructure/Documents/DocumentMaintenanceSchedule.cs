using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Defines the application-owned document retention schedule.</summary>
internal static class DocumentMaintenanceSchedule
{
    /// <summary>The database schema for scheduler metadata and the private app control audit.</summary>
    internal const string DurableSchema = "document_maintenance";

    /// <summary>The only retained jobs the Admin application may schedule.</summary>
    internal static IReadOnlyList<DocumentMaintenanceJob> Jobs
    {
        get;
    } =
    [
        DocumentMaintenanceJob.Drafts,
        DocumentMaintenanceJob.Audit,
        DocumentMaintenanceJob.Idempotency,
    ];

    /// <summary>Gets the one-day delay after a scheduled run completes.</summary>
    internal static TimeSpan FixedDelay
    {
        get;
    } = TimeSpan.FromDays(1);

    /// <summary>Resolves an allowlisted job to its stable scheduler name.</summary>
    internal static string GetJobName(DocumentMaintenanceJob job) => job switch
    {
        DocumentMaintenanceJob.Drafts => "admin-document-draft-retention",
        DocumentMaintenanceJob.Audit => "admin-document-audit-retention",
        DocumentMaintenanceJob.Idempotency => "admin-document-idempotency-retention",
        _ => throw new ArgumentOutOfRangeException(nameof(job), job, null),
    };
}
