using PandaBear.Branding;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Generates protected financial snapshots through existing document lineage, branding and draft retention.</summary>
public sealed class GeneratePaymentInstructionsCommandHandler(PaymentCommandExecutor executor, ITourStore tours, IPaymentAccountStore accounts,
    IDocumentStore documents, IPaymentInstructionStore instructions, IBrandingApiClient branding, TimeProvider clock)
{
    /// <summary>Creates an immutable draft; no generated financial field is staff-editable.</summary>
    public Task<Result<Guid>> Handle(GeneratePaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Input.InstallmentIds is null)
        {
            return Task.FromResult(FinancialErrors.Invalid("installments").ConvertError<Guid>());
        }
        var input = command.Input with
        {
            InstallmentIds = command.Input.InstallmentIds.Order().ToArray()
        };
        var fingerprint = PaymentCommandFingerprint.Create(command.BookingId, command.ReplacesDocumentId, command.ActorId, input, PaymentFinancialContractJsonContext.Default.GeneratePaymentInstructionsDto);
        return executor.Execute("payment-instructions:generate:" + command.BookingId.ToString("N"), command.IdempotencyKey, fingerprint,
            ["admin:booking-finance:" + command.BookingId.ToString("N"), "admin:payment-account:" + input.AccountId.ToString("N")], async () =>
        {
            var tour = await tours.GetByBookingId(command.BookingId, ct);
            var booking = tour?.Bookings.SingleOrDefault(item => item.Id == command.BookingId);
            var account = await accounts.GetById(input.AccountId, ct);
            if (tour is null || booking is null || account is null)
            {
                return Result.NotFound(detail: "Payment instruction source not found.").ConvertError<Guid>();
            }
            if (booking.FinancialHistory?.Version != input.ExpectedVersion || account.Version != input.ExpectedAccountVersion)
            {
                return FinancialErrors.Changed().ConvertError<Guid>();
            }
            var capturedBranding = await DocumentBrandingSnapshotFactory.Capture(branding, ct);
            var now = clock.GetUtcNow();
            var audit = DocumentAuditContext.Create(command.ActorId, Guid.CreateVersion7().ToString("N"));
            if (audit.IsFailure)
            {
                return audit.ConvertError<DocumentAuditContext, Guid>();
            }
            var frozen = tour.CreateBookingPaymentInstruction(command.BookingId, account, input.InstallmentIds, input.QuoteId, "VT-" + Guid.CreateVersion7().ToString("N"), now);
            if (frozen.IsFailure)
            {
                return frozen.ConvertError<FrozenPaymentInstruction, Guid>();
            }
            var content = PaymentInstructionContentFactory.Create(frozen.Value, capturedBranding);
            if (content.IsFailure)
            {
                return content.ConvertError<DocumentDraftContent, Guid>();
            }
            DocumentDraft document;
            if (command.ReplacesDocumentId is { } previousId)
            {
                var lineage = await documents.GetByDocumentId(previousId, ct);
                if (lineage is null || lineage.BookingId != command.BookingId || !lineage.RequiresPaymentInstructionWorkflow)
                {
                    return Result.NotFound(detail: "Payment instruction lineage not found.").ConvertError<Guid>();
                }
                if (input.ExpectedDocumentVersion != lineage.Version)
                {
                    return FinancialErrors.Changed().ConvertError<Guid>();
                }
                var next = lineage.CreateRevision(previousId, content.Value, now.UtcDateTime, audit.Value);
                if (next.IsFailure)
                {
                    return next.ConvertError<DocumentDraft, Guid>();
                }
                document = next.Value;
            }
            else
            {
                if ((await instructions.GetDocumentIds(command.BookingId, ct)).Count > 0)
                {
                    return Result.Conflict(detail: "Payment instructions already exist. Explicitly regenerate the current revision.").ConvertError<Guid>();
                }
                if (input.ExpectedDocumentVersion is not null)
                {
                    return FinancialErrors.Invalid("documentVersion").ConvertError<Guid>();
                }
                var created = DocumentLineage.Create(command.BookingId, DocumentType.PaymentInstructions, DocumentAudience.Customer, content.Value, now.UtcDateTime, audit.Value);
                if (created.IsFailure)
                {
                    return created.ConvertError<DocumentLineage, Guid>();
                }
                documents.Add(created.Value);
                document = created.Value.Revisions.Single();
            }
            await instructions.SaveSnapshot(document, frozen.Value, account, command.ActorId, now, ct);
            instructions.AddAudit(command.BookingId, new PaymentInstructionAuditMetadata(document.Id, document.Revision, command.ActorId, now,
                PaymentInstructionAuditOperation.Generated, FinancialReason.InstructionGenerated, false));
            return Result.Ok(document.Id);
        }, ct);
    }
}
