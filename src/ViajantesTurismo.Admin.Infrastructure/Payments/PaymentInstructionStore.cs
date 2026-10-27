using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Stores protected payment instruction snapshots, audit rows and artifacts through the write context.</summary>
/// <param name="context">The Admin write database context.</param>
/// <param name="protection">Provides the data protectors for snapshots and artifacts.</param>
internal sealed class PaymentInstructionStore(AdminWriteDbContext context, PaymentProtection protection) : IPaymentInstructionStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetDocumentIds(Guid bookingId, CancellationToken ct) => await context.Set<PaymentInstructionRow>().AsNoTracking()
        .Where(row => row.BookingId == bookingId).OrderByDescending(row => row.Revision).Select(row => row.DocumentId).ToArrayAsync(ct);

    /// <inheritdoc />
    public async Task<PaymentInstructionSnapshot?> GetSnapshot(Guid documentId, CancellationToken ct)
    {
        var row = await context.Set<PaymentInstructionRow>().AsNoTracking().SingleOrDefaultAsync(item => item.DocumentId == documentId, ct);
        if (row is null)
        {
            return null;
        }
        var document = await context.DocumentDrafts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == documentId, ct);
        var plaintext = protection.InstructionSnapshots.Unprotect(documentId.ToString("N"), row.ProtectedSnapshot);
        try
        {
            var state = JsonSerializer.Deserialize(plaintext, PaymentPersistenceJsonContext.Default.PaymentInstructionSnapshotState);
            if (state is null || document is null || state.SchemaVersion != 1 || state.DocumentId != row.DocumentId || state.Revision != row.Revision
                || state.Terms is null || state.Recipient is null || state.Terms.BookingId != row.BookingId || state.Terms.AccountId != row.AccountId
                || state.Terms.AccountRevisionId != row.AccountRevisionId || document.BookingId != row.BookingId || document.Revision != row.Revision
                || document.Type != DocumentType.PaymentInstructions || string.IsNullOrWhiteSpace(state.CreatedBy) || state.CreatedBy.Length > 256
                || state.CreatedAt != state.Terms.CreatedAt || // PostgreSQL timestamps retain microseconds; preserve the full instant inside the protected snapshot.
                state.CreatedAt.UtcTicks / 10 != document.CreatedAt.Ticks / 10)
            {
                throw new CryptographicException("Protected payment instruction state is invalid.");
            }
            var terms = FrozenPaymentInstruction.Restore(state.Terms);
            var recipient = PaymentRecipientDetails.Restore(state.Recipient);
            if (terms.IsFailure || recipient.IsFailure || recipient.Value.SettlementCurrency != terms.Value.Currency)
            {
                throw new CryptographicException("Protected payment instruction terms are invalid.");
            }
            return new PaymentInstructionSnapshot(documentId, row.Revision, terms.Value, recipient.Value, state.CreatedBy, state.CreatedAt,
                document.FinalizedAt is { } finalized ? new DateTimeOffset(finalized, TimeSpan.Zero) : null);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    /// <inheritdoc />
    public Task SaveSnapshot(DocumentDraft document, FrozenPaymentInstruction terms, PaymentAccount account, string actorId, DateTimeOffset now, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (document.Type != DocumentType.PaymentInstructions || document.BookingId != terms.BookingId || account.Id != terms.AccountId
            || !account.CanDeliver(terms.AccountRevisionId))
        {
            throw new InvalidOperationException("Payment instruction snapshot does not match its approved sources.");
        }
        var state = new PaymentInstructionSnapshotState(1, document.Id, document.Revision, terms.ExportState(), account.Current.Recipient.ExportState(), actorId, now);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(state, PaymentPersistenceJsonContext.Default.PaymentInstructionSnapshotState);
        try
        {
            context.Add(new PaymentInstructionRow
            {
                DocumentId = document.Id,
                BookingId = document.BookingId,
                AccountId = account.Id,
                AccountRevisionId = terms.AccountRevisionId,
                Revision = document.Revision,
                ProtectedSnapshot = protection.InstructionSnapshots.Protect(document.Id.ToString("N"), plaintext)
            });
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void AddAudit(Guid bookingId, PaymentInstructionAuditMetadata metadata) => context.Add(new PaymentAuditRow
    {
        Id = Guid.CreateVersion7(),
        ResourceId = bookingId,
        TargetId = metadata.DocumentId,
        Revision = metadata.Revision,
        ActorId = metadata.ActorId,
        OccurredAt = metadata.OccurredAt,
        Operation = "Instruction" + metadata.Operation,
        Reason = metadata.Reason.ToString(),
        SelfApproved = metadata.SelfApproved
    });

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentInstructionAuditMetadata>> GetAudits(Guid documentId, CancellationToken ct)
    {
        var rows = await context.Set<PaymentAuditRow>().AsNoTracking().Where(row => row.TargetId == documentId && row.Operation.StartsWith("Instruction"))
            .OrderBy(row => row.Sequence).ToArrayAsync(ct);
        return rows.Select(row => new PaymentInstructionAuditMetadata(documentId, checked((int)row.Revision), row.ActorId, row.OccurredAt,
            Enum.Parse<PaymentInstructionAuditOperation>(row.Operation["Instruction".Length..]), Enum.Parse<FinancialReason>(row.Reason), row.SelfApproved)).ToArray();
    }

    /// <inheritdoc />
    public byte[] ProtectArtifact(Guid documentId, byte[] artifact) => protection.InstructionArtifacts.Protect(documentId.ToString("N"), artifact);

    /// <inheritdoc />
    public async Task<byte[]?> GetArtifact(Guid documentId, CancellationToken ct)
    {
        var document = await context.DocumentDrafts.SingleOrDefaultAsync(item => item.Id == documentId, ct);
        var ciphertext = document?.GetFinalizedArtifactContent();
        return document?.Type == DocumentType.PaymentInstructions && ciphertext is { } content
            ? protection.InstructionArtifacts.Unprotect(documentId.ToString("N"), content.ToArray()) : null;
    }
}
