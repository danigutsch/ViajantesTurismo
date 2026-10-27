using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

/// <summary>Payment instruction store with no data that fails on any write.</summary>
internal sealed class EmptyPaymentInstructionStore : IPaymentInstructionStore
{
    /// <summary>Returns no document ids.</summary>
    public Task<IReadOnlyList<Guid>> GetDocumentIds(Guid bookingId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([]);
    /// <summary>Returns no snapshot.</summary>
    public Task<PaymentInstructionSnapshot?> GetSnapshot(Guid documentId, CancellationToken ct) => Task.FromResult<PaymentInstructionSnapshot?>(null);
    /// <summary>Always throws because the HTTP boundary test must not write.</summary>
    public Task SaveSnapshot(DocumentDraft document, FrozenPaymentInstruction terms, PaymentAccount account, string actorId, DateTimeOffset now, CancellationToken ct) => throw new InvalidOperationException("Unexpected write in HTTP boundary test.");
    /// <summary>Always throws because the HTTP boundary test must not write artifacts.</summary>
    public byte[] ProtectArtifact(Guid documentId, byte[] artifact) => throw new InvalidOperationException("Unexpected artifact write in HTTP boundary test.");
    /// <summary>Always throws because the HTTP boundary test must not write audits.</summary>
    public void AddAudit(Guid bookingId, PaymentInstructionAuditMetadata metadata) => throw new InvalidOperationException("Unexpected audit write in HTTP boundary test.");
    /// <summary>Returns no audits.</summary>
    public Task<IReadOnlyList<PaymentInstructionAuditMetadata>> GetAudits(Guid documentId, CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentInstructionAuditMetadata>>([]);
    /// <summary>Returns no artifact.</summary>
    public Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}
