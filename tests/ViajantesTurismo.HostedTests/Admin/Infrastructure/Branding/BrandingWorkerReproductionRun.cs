using System.Diagnostics;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Observations from one independently reset worker reproduction.</summary>
/// <param name="AttemptId">The fixture's opaque attempt ID.</param>
/// <param name="TraceId">The trace observed on the real retry log.</param>
/// <param name="StartedUtc">When the run started.</param>
/// <param name="DurationMs">The elapsed run time.</param>
/// <param name="InitialProjection">The stored projection immediately after the fence.</param>
/// <param name="Recovery">The recovery status and audit count before retry.</param>
/// <param name="Blocked">The persisted failure and retry state.</param>
/// <param name="Authorized">Whether the application accepted the synthetic reseed request.</param>
/// <param name="Retried">The persisted delivery state after retry.</param>
/// <param name="Duplicate">The persisted state after the duplicate delivery.</param>
/// <param name="FinalProjection">The projection after the worker settled the duplicate.</param>
/// <param name="FinalRecovery">The recovery state after the retry.</param>
internal sealed record BrandingWorkerReproductionRun(
    Guid AttemptId,
    ActivityTraceId TraceId,
    DateTimeOffset StartedUtc,
    int DurationMs,
    (long Revision, string BrandName, long InboxMessages) InitialProjection,
    (string Status, long AuditEntries) Recovery,
    BrandingWorkerDelivery Blocked,
    bool Authorized,
    BrandingWorkerDelivery Retried,
    BrandingWorkerDelivery Duplicate,
    (long Revision, string BrandName, long InboxMessages) FinalProjection,
    (string Status, long AuditEntries) FinalRecovery);
