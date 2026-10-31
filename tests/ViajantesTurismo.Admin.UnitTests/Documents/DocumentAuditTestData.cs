using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Documents;

/// <summary>Test data for document audit context and writer.</summary>
internal static class DocumentAuditTestData
{
    /// <summary>Creates a valid document audit context with fixed actor and correlation ids.</summary>
    public static DocumentAuditContext CreateContext() => DocumentAuditContext.Create(
        "9c5ff2e6-8b35-4f78-9df3-ef15af8e92a4",
        "9a3ca841b4354928861c660a6e4e1b99").Value;

    /// <summary>Creates an audit writer over the given fakes using the system clock.</summary>
    public static DocumentAuditWriter CreateWriter(
        FakeDocumentAuditStore auditStore,
        FakeUnitOfWork unitOfWork) => new(auditStore, unitOfWork, TimeProvider.System);

    /// <summary>Creates a purge command with the fixed audit context.</summary>
    public static PurgeExpiredDocumentAuditsCommand CreatePurgeCommand() => new(CreateContext());

    /// <summary>Creates a purge handler over the given fakes with a clock fixed at <paramref name="now" />.</summary>
    public static PurgeExpiredDocumentAuditsCommandHandler CreatePurgeHandler(
        FakeDocumentAuditStore auditStore,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        FakeDispositionHoldGuard? guard = null,
        InMemoryDispositionHoldStore? holds = null)
    {
        var clock = new FakeTimeProvider(now);
        var gate = new DocumentHoldGate(guard ?? new FakeDispositionHoldGuard(), holds ?? new InMemoryDispositionHoldStore(), clock);
        return new(auditStore, unitOfWork, new FakeDocumentDispositionTransaction(), gate, clock);
    }
}
