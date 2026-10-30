using System.Security.Cryptography;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Coordinates revision-preconditioned instruction approval, protected finalization and revalidated authenticated delivery.</summary>
public sealed class PaymentInstructionWorkflow(PaymentCommandExecutor commands, IPaymentTransaction transaction, IPaymentInstructionStore instructions,
    IPaymentAccountStore accounts, IDocumentStore documents, IDocumentQueryService queries, ITourStore tours, IUnitOfWork unitOfWork, TimeProvider clock)
{
    /// <summary>Applies an explicit confirmed lifecycle action; independent review uses a separate operation.</summary>
    /// <remarks>Finalization rechecks eligibility after all preparation, so no expired quote or retired recipient may be finalized.</remarks>
    public async Task<Result<Guid>> Change(Guid documentId, UpdatePaymentInstructionsDto input, string actorId, string? key, PaymentInstructionAuditOperation operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var expectedReason = operation switch
        {
            PaymentInstructionAuditOperation.Approved => FinancialReasonDto.InstructionApproved,
            PaymentInstructionAuditOperation.Finalized => FinancialReasonDto.InstructionFinalized,
            PaymentInstructionAuditOperation.ApprovalWithdrawn => FinancialReasonDto.ApprovalWithdrawn,
            PaymentInstructionAuditOperation.Voided => FinancialReasonDto.InstructionVoided,
            _ => FinancialReasonDto.None
        };
        if (!input.Confirmed || input.Reason != expectedReason || expectedReason == FinancialReasonDto.None)
        {
            return FinancialErrors.Invalid("confirmation").ConvertError<Guid>();
        }
        var initial = await instructions.GetSnapshot(documentId, ct);
        if (initial is null)
        {
            return Result.NotFound(detail: "Payment instruction not found.").ConvertError<Guid>();
        }
        var fingerprint = PaymentCommandFingerprint.Create(initial.Terms.BookingId, documentId, actorId, input, PaymentFinancialContractJsonContext.Default.UpdatePaymentInstructionsDto);
        return await commands.Execute("payment-instruction:" + operation + ":" + documentId.ToString("N"), key, fingerprint, Resources(initial), async () =>
        {
            var loaded = await Load(documentId, ct);
            if (loaded.IsFailure)
            {
                return loaded.ConvertError<PaymentInstructionSources, Guid>();
            }
            var current = loaded.Value;
            if (current.Lineage.Version != input.ExpectedDocumentVersion)
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            if (operation is PaymentInstructionAuditOperation.Approved or PaymentInstructionAuditOperation.Finalized && !await Eligible(current, ct))
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            var now = clock.GetUtcNow();
            var audit = DocumentAuditContext.Create(actorId, Guid.CreateVersion7().ToString("N"));
            if (audit.IsFailure)
            {
                return audit.ConvertError<DocumentAuditContext, Guid>();
            }
            Result result;
            switch (operation)
            {
                case PaymentInstructionAuditOperation.Approved:
                    if (current.Document.CanBeginReview)
                    {
                        result = current.Lineage.BeginReview(documentId, now.UtcDateTime, audit.Value);
                        if (result.IsFailure)
                        {
                            return result.ConvertError<Guid>();
                        }
                    }
                    result = current.Lineage.Approve(documentId, now.UtcDateTime, audit.Value);
                    break;
                case PaymentInstructionAuditOperation.Finalized:
                    var audits = await instructions.GetAudits(documentId, ct);
                    if (audits.LastOrDefault(item => item.Operation is PaymentInstructionAuditOperation.Approved or PaymentInstructionAuditOperation.ApprovalWithdrawn)?.Operation != PaymentInstructionAuditOperation.Approved)
                    {
                        return FinancialErrors.Unavailable().ConvertError<Guid>();
                    }
                    if (!await Eligible(current, ct))
                    {
                        return FinancialErrors.Changed().ConvertError<Guid>();
                    }
                    now = clock.GetUtcNow();
                    var plaintext = PaymentInstructionArtifactRenderer.Render(current.Document, current.Snapshot);
                    try
                    {
                        result = current.Lineage.Finalize(documentId, instructions.ProtectArtifact(documentId, plaintext), now.UtcDateTime, audit.Value);
                    }
                    finally { CryptographicOperations.ZeroMemory(plaintext); }
                    break;
                case PaymentInstructionAuditOperation.ApprovalWithdrawn:
                    result = current.Lineage.WithdrawApproval(documentId, now.UtcDateTime, audit.Value);
                    break;
                case PaymentInstructionAuditOperation.Voided:
                    result = current.Lineage.Void(documentId, DocumentVoidReason.AdministrativeVoid, now.UtcDateTime, audit.Value);
                    break;
                default:
                    return FinancialErrors.Invalid("operation").ConvertError<Guid>();
            }
            if (result.IsFailure)
            {
                return result.ConvertError<Guid>();
            }
            if (operation == PaymentInstructionAuditOperation.Finalized && !await Eligible(current, ct))
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            instructions.AddAudit(current.Document.BookingId, new PaymentInstructionAuditMetadata(documentId, current.Document.Revision, actorId, now,
                operation, FinancialMappings.Map(input.Reason), operation == PaymentInstructionAuditOperation.Approved && actorId == current.Snapshot.CreatedBy));
            return Result.Ok(documentId);
        }, ct);
    }

    /// <summary>Records optional independent review without invoking document approval withdrawal.</summary>
    public async Task<Result<Guid>> Review(Guid documentId, ReviewPaymentInstructionsDto input, string actorId, string? key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var initial = await instructions.GetSnapshot(documentId, ct);
        if (initial is null)
        {
            return Result.NotFound(detail: "Payment instruction not found.").ConvertError<Guid>();
        }
        var fingerprint = PaymentCommandFingerprint.Create(initial.Terms.BookingId, documentId, actorId, input, PaymentFinancialContractJsonContext.Default.ReviewPaymentInstructionsDto);
        return await commands.Execute("payment-instruction:review:" + documentId.ToString("N"), key, fingerprint, Resources(initial), async () =>
        {
            var loaded = await Load(documentId, ct);
            if (loaded.IsFailure)
            {
                return loaded.ConvertError<PaymentInstructionSources, Guid>();
            }
            var current = loaded.Value;
            if (current.Lineage.Version != input.ExpectedDocumentVersion)
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            var decision = FinancialReviewDecision.Create(documentId, current.Document.Revision, current.Snapshot.CreatedBy, actorId, clock.GetUtcNow(), input.ChangesRequested);
            if (decision.IsFailure)
            {
                return decision.ConvertError<FinancialReviewDecision, Guid>();
            }
            instructions.AddAudit(current.Document.BookingId, new PaymentInstructionAuditMetadata(documentId, current.Document.Revision, actorId, decision.Value.ReviewedAt,
                input.ChangesRequested ? PaymentInstructionAuditOperation.ReviewChangesRequested : PaymentInstructionAuditOperation.ReviewAccepted,
                input.ChangesRequested ? FinancialReason.ReviewChangesRequested : FinancialReason.ReviewAccepted, false));
            return Result.Ok(documentId);
        }, ct);
    }

    /// <summary>Rechecks eligibility under booking/account locks before releasing a protected artifact through the existing download proxy.</summary>
    public async Task<Result<FinalizedDocumentArtifact>> Download(Guid documentId, string? actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return FinancialErrors.Invalid("actor").ConvertError<FinalizedDocumentArtifact>();
        }
        var initial = await instructions.GetSnapshot(documentId, ct);
        if (initial is null)
        {
            return Result.NotFound(detail: "Payment instruction not found.").ConvertError<FinalizedDocumentArtifact>();
        }
        FinalizedDocumentArtifact? artifact = null;
        var result = await transaction.Execute(Resources(initial), async () =>
        {
            var loaded = await Load(documentId, ct);
            if (loaded.IsFailure)
            {
                return loaded.ConvertError<PaymentInstructionSources, Guid>();
            }
            var current = loaded.Value;
            if (!current.Document.IsFinalized || !await Eligible(current, ct))
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            var bytes = await instructions.GetArtifact(documentId, ct);
            if (bytes is null || string.IsNullOrWhiteSpace(current.Document.FinalizedArtifactName))
            {
                return FinancialErrors.Unavailable().ConvertError<Guid>();
            }
            artifact = new FinalizedDocumentArtifact(documentId, current.Document.BookingId, current.Document.Revision, bytes, current.Document.FinalizedArtifactName);
            instructions.AddAudit(current.Document.BookingId, new PaymentInstructionAuditMetadata(documentId, current.Document.Revision, actorId, clock.GetUtcNow(),
                PaymentInstructionAuditOperation.Downloaded, FinancialReason.InstructionDownloaded, false));
            await unitOfWork.SaveEntities(ct);
            if (!await Eligible(current, ct))
            {
                CryptographicOperations.ZeroMemory(bytes);
                artifact = null;
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            return Result.Ok(documentId);
        }, ct);
        if (result.IsFailure)
        {
            return result.ConvertError<Guid, FinalizedDocumentArtifact>();
        }
        return artifact is not null ? Result.Ok(artifact) : FinancialErrors.Unavailable().ConvertError<FinalizedDocumentArtifact>();
    }

    /// <summary>Lists retained booking instruction revisions without exposing recipient details.</summary>
    public async Task<IReadOnlyList<GetPaymentInstructionDto>> ListForBooking(Guid bookingId, CancellationToken ct)
    {
        var results = new List<GetPaymentInstructionDto>();
        foreach (var id in await instructions.GetDocumentIds(bookingId, ct))
        {
            var item = await GetById(id, ct);
            if (item is not null)
            {
                results.Add(item);
            }
        }
        return results;
    }

    /// <summary>Projects generated financial fields and masked recipient metadata for Admin confirmation.</summary>
    public async Task<GetPaymentInstructionDto?> GetById(Guid documentId, CancellationToken ct)
    {
        var loaded = await Load(documentId, ct);
        if (loaded.IsFailure)
        {
            return null;
        }
        var current = loaded.Value;
        var dto = await queries.GetById(documentId, ct);
        if (dto is null)
        {
            return null;
        }
        var audits = await instructions.GetAudits(documentId, ct);
        var approval = audits.LastOrDefault(item => item.Operation == PaymentInstructionAuditOperation.Approved);
        return new GetPaymentInstructionDto
        {
            Document = dto,
            DocumentVersion = current.Lineage.Version,
            AccountId = current.Snapshot.Terms.AccountId,
            AccountRevisionId = current.Snapshot.Terms.AccountRevisionId,
            MaskedRecipient = current.Snapshot.Recipient.MaskedIdentifier,
            Eligible = !current.Document.IsRetired && await Eligible(current, ct),
            EuroAmount = current.Snapshot.Terms.EuroAmount,
            SettlementAmount = current.Snapshot.Terms.SettlementAmount,
            SettlementCurrency = TourMapper.MapToCurrencyDto(current.Snapshot.Terms.Currency),
            PaymentReference = current.Snapshot.Terms.Reference,
            QuoteExpiresAt = current.Snapshot.Terms.QuoteExpiresAt,
            Approval = new FinancialApprovalDto
            {
                CreatedBy = current.Snapshot.CreatedBy,
                CreatedAt = current.Snapshot.CreatedAt,
                ApprovedBy = approval?.ActorId,
                ApprovedAt = approval?.OccurredAt,
                ApprovalReason = approval is null ? null : FinancialReasonDto.InstructionApproved,
                SelfApproved = approval?.SelfApproved ?? false
            },
            Reviews = [.. audits.Where(item => item.Operation is PaymentInstructionAuditOperation.ReviewAccepted or PaymentInstructionAuditOperation.ReviewChangesRequested)
                .Select(item => new GetFinancialReviewDto
                {
                    TargetId = documentId,
                    TargetRevision = item.Revision,
                    ReviewerId = item.ActorId,
                    ReviewedAt = item.OccurredAt,
                    ChangesRequested = item.Operation == PaymentInstructionAuditOperation.ReviewChangesRequested
                })]
        };
    }

    private async Task<Result<PaymentInstructionSources>> Load(Guid documentId, CancellationToken ct)
    {
        var lineage = await documents.GetByDocumentId(documentId, ct);
        var document = lineage?.GetRevision(documentId);
        var snapshot = await instructions.GetSnapshot(documentId, ct);
        return lineage is not null && document is not null && snapshot is not null && document.RequiresPaymentInstructionWorkflow
            ? Result.Ok(new PaymentInstructionSources(lineage, document, snapshot)) : Result.NotFound(detail: "Payment instruction not found.").ConvertError<PaymentInstructionSources>();
    }
    private async Task<bool> Eligible(PaymentInstructionSources current, CancellationToken ct)
    {
        var tour = await tours.GetByBookingId(current.Document.BookingId, ct);
        var account = await accounts.GetById(current.Snapshot.Terms.AccountId, ct);
        return tour is not null && account is not null && tour.CanDeliverBookingPaymentInstruction(current.Snapshot.Terms, account, clock.GetUtcNow());
    }
    private static string[] Resources(PaymentInstructionSnapshot snapshot) =>
        ["admin:booking-finance:" + snapshot.Terms.BookingId.ToString("N"), "admin:payment-account:" + snapshot.Terms.AccountId.ToString("N")];
}
