using PandaBear.BuildingBlocks;
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
    /// <summary>Returns no document ids and a single empty page.</summary>
    public Task<(IReadOnlyList<Guid> Items, PageWindow Window)> GetDocumentIdPage(Guid bookingId, int? page, int pageSize, CancellationToken ct) =>
        Task.FromResult<(IReadOnlyList<Guid>, PageWindow)>(([], PageWindow.Create(page, pageSize, 0)));
    /// <summary>Returns no document state.</summary>
    public Task<(long Version, bool IsRetired)?> GetDocumentState(Guid documentId, CancellationToken ct) =>
        Task.FromResult<(long, bool)?>(null);
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
    /// <inheritdoc />
    public Task<PaymentInstructionAuditMetadata?> GetLatestAudit(Guid documentId, IReadOnlyList<PaymentInstructionAuditOperation> operations, CancellationToken ct) =>
        Task.FromResult<PaymentInstructionAuditMetadata?>(null);
    /// <inheritdoc />
    public Task<(IReadOnlyList<PaymentInstructionAuditMetadata> Items, PageWindow Window)?> GetReviewPage(Guid documentId, int? page, int pageSize, CancellationToken ct) =>
        Task.FromResult<(IReadOnlyList<PaymentInstructionAuditMetadata>, PageWindow)?>(null);
    /// <summary>Returns no artifact.</summary>
    public Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct) => Task.FromResult<byte[]?>(null);
}
