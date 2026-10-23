using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.ApiServiceTests.Payments;

internal sealed class EmptyPaymentInstructionStore : IPaymentInstructionStore
{
    public Task<IReadOnlyList<Guid>> GetDocumentIds(Guid bookingId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([]);
    public Task<PaymentInstructionSnapshot?> GetSnapshot(Guid documentId, CancellationToken ct) => Task.FromResult<PaymentInstructionSnapshot?>(null);
    public Task SaveSnapshot(DocumentDraft document, FrozenPaymentInstruction terms, PaymentAccount account, string actorId, DateTimeOffset now, CancellationToken ct) => throw new InvalidOperationException("Unexpected write in HTTP boundary test.");
    public byte[] ProtectArtifact(Guid documentId, byte[] artifact) => throw new InvalidOperationException("Unexpected artifact write in HTTP boundary test.");
    public void AddAudit(Guid bookingId, PaymentInstructionAuditMetadata metadata) => throw new InvalidOperationException("Unexpected audit write in HTTP boundary test.");
    public Task<IReadOnlyList<PaymentInstructionAuditMetadata>> GetAudits(Guid documentId, CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentInstructionAuditMetadata>>([]);
    public Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}
