using SharedKernel.AuditTrail;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class CapturingDocumentAuditTrailSink : IAuditTrailSink<DocumentAuditRecord>
{
    private readonly List<DocumentAuditRecord> entries = [];

    public IReadOnlyList<DocumentAuditRecord> Entries => entries;

    public ValueTask Append(DocumentAuditRecord entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        entries.Add(entry);
        return ValueTask.CompletedTask;
    }
}
