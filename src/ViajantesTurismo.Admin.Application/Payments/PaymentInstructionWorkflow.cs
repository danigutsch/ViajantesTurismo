using System.Security.Cryptography;
using SharedKernel.Idempotency;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Application.Tours;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Coordinates revision-preconditioned instruction approval, protected finalization and revalidated authenticated delivery.</summary>
public sealed class PaymentInstructionWorkflow(ISender sender, PaymentCommandIdempotency commands, IPaymentInstructionStore instructions,
    IPaymentAccountStore accounts, IDocumentStore documents, IDocumentQueryService queries, ITourStore tours, ITourCommandStore commandTours, IUnitOfWork unitOfWork, TimeProvider clock)
{
    /// <summary>Applies an explicit confirmed lifecycle action; independent review uses a separate operation.</summary>
    /// <remarks>Finalization rechecks eligibility after all preparation, so no expired quote or retired recipient may be finalized.</remarks>
    public async ValueTask<Result<Guid>> Change(Guid documentId, UpdatePaymentInstructionsDto input, string actorId, IdempotencyKey key, PaymentInstructionAuditOperation operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Confirmed(input, operation))
        {
            return FinancialErrors.Invalid("confirmation").ConvertError<Guid>();
        }
        var initial = await instructions.GetSnapshot(documentId, ct);
        if (initial is null)
        {
            return Result.NotFound(detail: "Payment instruction not found.").ConvertError<Guid>();
        }
        var target = TargetOf(initial);
        ICommand<Result<Guid>> command = operation switch
        {
            PaymentInstructionAuditOperation.Approved => new ApprovePaymentInstructionsCommand(target, input, actorId, key),
            PaymentInstructionAuditOperation.Finalized => new FinalizePaymentInstructionsCommand(target, input, actorId, key),
            PaymentInstructionAuditOperation.ApprovalWithdrawn => new WithdrawPaymentInstructionApprovalCommand(target, input, actorId, key),
            PaymentInstructionAuditOperation.Voided => new VoidPaymentInstructionsCommand(target, input, actorId, key),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        return await sender.Send(command, ct);
    }

    /// <summary>Applies the validated lifecycle operation after the transaction acquires its advisory locks.</summary>
    internal ValueTask<Result<Guid>> ApplyChange(PaymentInstructionTarget target, string scope, UpdatePaymentInstructionsDto input,
        string actorId, IdempotencyKey key, PaymentInstructionAuditOperation operation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Confirmed(input, operation))
        {
            return ValueTask.FromResult(FinancialErrors.Invalid("confirmation").ConvertError<Guid>());
        }
        var documentId = target.DocumentId;
        var fingerprint = PaymentCommandFingerprint.Create(target.BookingId, documentId, actorId, input, PaymentFinancialContractJsonContext.Default.UpdatePaymentInstructionsDto);
        return commands.Execute(scope, key, fingerprint, async () =>
        {
            var tour = await commandTours.GetByBookingIdForPaymentUpdate(target.BookingId, ct);
            var loaded = await Load(target, ct);
            if (loaded.IsFailure)
            {
                return loaded.ConvertError<PaymentInstructionSources, Guid>();
            }
            var current = loaded.Value;
            if (current.Lineage.Version != input.ExpectedDocumentVersion)
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            if (operation is PaymentInstructionAuditOperation.Approved or PaymentInstructionAuditOperation.Finalized && !await Eligible(current, tour, ct))
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
                    var approvalDecision = await instructions.GetLatestAudit(documentId,
                        [PaymentInstructionAuditOperation.Approved, PaymentInstructionAuditOperation.ApprovalWithdrawn], ct);
                    if (approvalDecision?.Operation != PaymentInstructionAuditOperation.Approved)
                    {
                        return FinancialErrors.Unavailable().ConvertError<Guid>();
                    }
                    if (!await Eligible(current, tour, ct))
                    {
                        return FinancialErrors.Changed().ConvertError<Guid>();
                    }
                    now = clock.GetUtcNow();
                    var plaintext = PaymentInstructionArtifactRenderer.Render(current.Document, current.Snapshot);
                    try
                    {
                        result = current.Lineage.Finalize(
                            documentId,
                            instructions.ProtectArtifact(documentId, plaintext),
                            now.UtcDateTime,
                            audit.Value,
                            DocumentArtifactRendererVersion.Current);
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
            if (operation == PaymentInstructionAuditOperation.Finalized && !await Eligible(current, tour, ct))
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            instructions.AddAudit(current.Document.BookingId, new PaymentInstructionAuditMetadata(documentId, current.Document.Revision, actorId, now,
                operation, FinancialMappings.Map(input.Reason), operation == PaymentInstructionAuditOperation.Approved && actorId == current.Snapshot.CreatedBy));
            return Result.Ok(documentId);
        }, ct);
    }

    /// <summary>Records optional independent review without invoking document approval withdrawal.</summary>
    public async ValueTask<Result<Guid>> Review(Guid documentId, ReviewPaymentInstructionsDto input, string actorId, IdempotencyKey key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var initial = await instructions.GetSnapshot(documentId, ct);
        if (initial is null)
        {
            return Result.NotFound(detail: "Payment instruction not found.").ConvertError<Guid>();
        }
        return await sender.Send(new ReviewPaymentInstructionsCommand(TargetOf(initial), input, actorId, key), ct);
    }

    /// <summary>Applies an independent review after the transaction acquires its advisory locks.</summary>
    internal ValueTask<Result<Guid>> ApplyReview(PaymentInstructionTarget target, string scope, ReviewPaymentInstructionsDto input,
        string actorId, IdempotencyKey key, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        var documentId = target.DocumentId;
        var fingerprint = PaymentCommandFingerprint.Create(target.BookingId, documentId, actorId, input, PaymentFinancialContractJsonContext.Default.ReviewPaymentInstructionsDto);
        return commands.Execute(scope, key, fingerprint, async () =>
        {
            _ = await commandTours.GetByBookingIdForPaymentUpdate(target.BookingId, ct);
            var loaded = await Load(target, ct);
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
        return await sender.Send(new DownloadPaymentInstructionsCommand(TargetOf(initial), actorId), ct);
    }

    /// <summary>Stages the authenticated download audit and revalidates artifact release.</summary>
    internal async ValueTask<Result<FinalizedDocumentArtifact>> ReadArtifact(PaymentInstructionTarget target, string actorId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            return FinancialErrors.Invalid("actor").ConvertError<FinalizedDocumentArtifact>();
        }
        var tour = await commandTours.GetByBookingIdForPaymentUpdate(target.BookingId, ct);
        var loaded = await Load(target, ct);
        if (loaded.IsFailure)
        {
            return loaded.ConvertError<PaymentInstructionSources, FinalizedDocumentArtifact>();
        }
        var current = loaded.Value;
        if (!current.Document.IsFinalized || !await Eligible(current, tour, ct))
        {
            return FinancialErrors.Changed().ConvertError<FinalizedDocumentArtifact>();
        }
        var fileName = current.Document.FinalizedArtifactName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return FinancialErrors.Unavailable().ConvertError<FinalizedDocumentArtifact>();
        }
        var bytes = await instructions.GetArtifact(target.DocumentId, ct);
        if (bytes is null)
        {
            return FinancialErrors.Unavailable().ConvertError<FinalizedDocumentArtifact>();
        }
        var release = false;
        try
        {
            instructions.AddAudit(target.BookingId, new PaymentInstructionAuditMetadata(target.DocumentId, target.Revision, actorId, clock.GetUtcNow(),
                PaymentInstructionAuditOperation.Downloaded, FinancialReason.InstructionDownloaded, false));
            await unitOfWork.SaveEntities(ct);
            if (!await Eligible(current, tour, ct))
            {
                return FinancialErrors.Changed().ConvertError<FinalizedDocumentArtifact>();
            }
            release = true;
            return Result.Ok(new FinalizedDocumentArtifact(target.DocumentId, target.BookingId, target.Revision, bytes, fileName));
        }
        finally
        {
            if (!release)
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }
    }

    /// <summary>Lists retained booking instruction revisions without exposing recipient details.</summary>
    public async Task<IReadOnlyList<GetPaymentInstructionDto>> ListForBooking(Guid bookingId, CancellationToken ct) =>
        await ProjectRevisions(await instructions.GetDocumentIds(bookingId, ct), latestReviewOnly: false, ct);

    /// <summary>Lists one page of retained booking instruction revisions without exposing recipient details.</summary>
    public Task<GetPaymentInstructionPageDto> ListPageForBooking(Guid bookingId, int? page, int pageSize, CancellationToken ct) =>
        ListPageForBooking(bookingId, page, pageSize, latestReviewOnly: false, ct);

    /// <summary>Lists one page of retained booking instructions with optional latest-review summaries.</summary>
    public async Task<GetPaymentInstructionPageDto> ListPageForBooking(Guid bookingId, int? page, int pageSize, bool latestReviewOnly, CancellationToken ct)
    {
        var (ids, window) = await instructions.GetDocumentIdPage(bookingId, page, pageSize, ct);
        return new GetPaymentInstructionPageDto
        {
            Items = await ProjectRevisions(ids, latestReviewOnly, ct),
            Page = window.Page,
            PageSize = window.PageSize,
            Total = window.Total
        };
    }

    /// <summary>Lists one page of independent reviews for the specified instruction revision.</summary>
    public async Task<GetPaymentInstructionReviewPageDto?> GetReviewPage(Guid documentId, int? page, int pageSize, CancellationToken ct)
    {
        var reviews = await instructions.GetReviewPage(documentId, page, pageSize, ct);
        return reviews is { } result ? new GetPaymentInstructionReviewPageDto
        {
            Items = [.. result.Items.Select(ProjectReview)],
            Page = result.Window.Page,
            PageSize = result.Window.PageSize,
            Total = result.Window.Total
        } : null;
    }

    private async Task<IReadOnlyList<GetPaymentInstructionDto>> ProjectRevisions(IReadOnlyList<Guid> ids, bool latestReviewOnly, CancellationToken ct)
    {
        var results = new List<GetPaymentInstructionDto>(ids.Count);
        foreach (var id in ids)
        {
            var item = await GetById(id, latestReviewOnly, ct);
            if (item is not null)
            {
                results.Add(item);
            }
        }
        return results;
    }

    /// <summary>Projects generated financial fields and masked recipient metadata for Admin confirmation.</summary>
    public Task<GetPaymentInstructionDto?> GetById(Guid documentId, CancellationToken ct) => GetById(documentId, latestReviewOnly: false, ct);

    /// <summary>Projects generated financial fields and masked recipient metadata with optional latest-review summary.</summary>
    public async Task<GetPaymentInstructionDto?> GetById(Guid documentId, bool latestReviewOnly, CancellationToken ct)
    {
        var state = await instructions.GetDocumentState(documentId, ct);
        var snapshot = await instructions.GetSnapshot(documentId, ct);
        if (state is not { } current || snapshot is null)
        {
            return null;
        }
        var dto = await queries.GetById(documentId, ct);
        if (dto is null)
        {
            return null;
        }
        var (approval, reviews) = await ProjectAudits(documentId, latestReviewOnly, ct);
        return new GetPaymentInstructionDto
        {
            Document = dto,
            DocumentVersion = current.Version,
            AccountId = snapshot.Terms.AccountId,
            AccountRevisionId = snapshot.Terms.AccountRevisionId,
            MaskedRecipient = snapshot.Recipient.MaskedIdentifier,
            Eligible = !current.IsRetired && await Eligible(snapshot, await tours.GetByBookingId(snapshot.Terms.BookingId, ct), ct),
            EuroAmount = snapshot.Terms.EuroAmount,
            SettlementAmount = snapshot.Terms.SettlementAmount,
            SettlementCurrency = snapshot.Terms.Currency.Code,
            PaymentReference = snapshot.Terms.Reference,
            QuoteExpiresAt = snapshot.Terms.QuoteExpiresAt,
            Approval = new FinancialApprovalDto
            {
                CreatedBy = snapshot.CreatedBy,
                CreatedAt = snapshot.CreatedAt,
                ApprovedBy = approval?.ActorId,
                ApprovedAt = approval?.OccurredAt,
                ApprovalReason = approval is null ? null : FinancialReasonDto.InstructionApproved,
                SelfApproved = approval?.SelfApproved ?? false
            },
            Reviews = reviews
        };
    }

    private async Task<(PaymentInstructionAuditMetadata? Approval, IReadOnlyList<GetFinancialReviewDto> Reviews)> ProjectAudits(Guid documentId, bool latestReviewOnly, CancellationToken ct)
    {
        if (latestReviewOnly)
        {
            var approval = await instructions.GetLatestAudit(documentId, [PaymentInstructionAuditOperation.Approved], ct);
            var review = await instructions.GetLatestAudit(documentId,
                [PaymentInstructionAuditOperation.ReviewAccepted, PaymentInstructionAuditOperation.ReviewChangesRequested], ct);
            return (approval, review is null ? [] : [ProjectReview(review)]);
        }

        var audits = await instructions.GetAudits(documentId, ct);
        return (audits.LastOrDefault(item => item.Operation == PaymentInstructionAuditOperation.Approved),
            [.. audits.Where(item => item.Operation is PaymentInstructionAuditOperation.ReviewAccepted or PaymentInstructionAuditOperation.ReviewChangesRequested).Select(ProjectReview)]);
    }

    private static GetFinancialReviewDto ProjectReview(PaymentInstructionAuditMetadata review) => new()
    {
        TargetId = review.DocumentId,
        TargetRevision = review.Revision,
        ReviewerId = review.ActorId,
        ReviewedAt = review.OccurredAt,
        ChangesRequested = review.Operation == PaymentInstructionAuditOperation.ReviewChangesRequested
    };

    private async Task<Result<PaymentInstructionSources>> Load(Guid documentId, CancellationToken ct)
    {
        var lineage = await documents.GetByDocumentId(documentId, ct);
        var document = lineage?.GetRevision(documentId);
        var snapshot = await instructions.GetSnapshot(documentId, ct);
        return lineage is not null && document is not null && snapshot is not null && document.RequiresPaymentInstructionWorkflow
            ? Result.Ok(new PaymentInstructionSources(lineage, document, snapshot)) : Result.NotFound(detail: "Payment instruction not found.").ConvertError<PaymentInstructionSources>();
    }
    private async Task<Result<PaymentInstructionSources>> Load(PaymentInstructionTarget target, CancellationToken ct)
    {
        var loaded = await Load(target.DocumentId, ct);
        return loaded.IsSuccess && TargetOf(loaded.Value.Snapshot) != target
            ? FinancialErrors.Changed().ConvertError<PaymentInstructionSources>()
            : loaded;
    }

    private Task<bool> Eligible(PaymentInstructionSources current, Tour? tour, CancellationToken ct) =>
        Eligible(current.Snapshot, tour, ct);

    private async Task<bool> Eligible(PaymentInstructionSnapshot snapshot, Tour? tour, CancellationToken ct)
    {
        var account = await accounts.GetById(snapshot.Terms.AccountId, ct);
        return tour is not null && account is not null && tour.CanDeliverBookingPaymentInstruction(snapshot.Terms, account, clock.GetUtcNow());
    }

    private static PaymentInstructionTarget TargetOf(PaymentInstructionSnapshot snapshot) =>
        new(snapshot.DocumentId, snapshot.Terms.BookingId, snapshot.Terms.AccountId, snapshot.Revision);

    private static bool Confirmed(UpdatePaymentInstructionsDto input, PaymentInstructionAuditOperation operation)
    {
        var expectedReason = operation switch
        {
            PaymentInstructionAuditOperation.Approved => FinancialReasonDto.InstructionApproved,
            PaymentInstructionAuditOperation.Finalized => FinancialReasonDto.InstructionFinalized,
            PaymentInstructionAuditOperation.ApprovalWithdrawn => FinancialReasonDto.ApprovalWithdrawn,
            PaymentInstructionAuditOperation.Voided => FinancialReasonDto.InstructionVoided,
            _ => FinancialReasonDto.None
        };
        return input.Confirmed && input.Reason == expectedReason && expectedReason != FinancialReasonDto.None;
    }
}
