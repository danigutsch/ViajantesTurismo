using PandaBear.AuditTrail;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>Audit trail sink that keeps appended document audit records in memory for assertions.</summary>
internal sealed class CapturingDocumentAuditTrailSink : IAuditTrailSink<DocumentAuditRecord>
{
    private readonly List<DocumentAuditRecord> entries = [];

    /// <summary>Gets the appended audit records in append order.</summary>
    public IReadOnlyList<DocumentAuditRecord> Entries => entries;

    /// <summary>Adds the record to the captured entries, honoring cancellation.</summary>
    public ValueTask Append(DocumentAuditRecord entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        entries.Add(entry);
        return ValueTask.CompletedTask;
    }
}
