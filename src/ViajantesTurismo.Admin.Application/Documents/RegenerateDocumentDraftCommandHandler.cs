using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Creates a new document revision without mutating prior artifacts.</summary>
public sealed class RegenerateDocumentDraftCommandHandler(
    IDocumentStore documentStore,
    IQueryService queryService,
    ITourStore tourStore,
    DocumentBrandingSource documentBrandingSource,
    TimeProvider timeProvider,
    DocumentAuditWriter documentAuditWriter,
    DocumentCommandIdempotency documentCommandIdempotency)
{
    /// <summary>Generates and persists the replacement revision.</summary>
    public Task<Result<Guid>> Handle(RegenerateDocumentDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return documentCommandIdempotency.Execute(
            command, documentAuditWriter, commit => Prepare(command, commit, ct), ct);
    }

    private async Task<Result<Guid>> Prepare(
        RegenerateDocumentDraftCommand command,
        DocumentCommandCommit commit,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lineage = await documentStore.GetByDocumentId(command.DocumentId, ct);
        var current = lineage?.GetRevision(command.DocumentId);
        if (lineage is null || current is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound().ConvertError<Guid>(),
                command.AuditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                ct);
        }

        if (lineage.RequiresPaymentInstructionWorkflow)
        {
            return Domain.Payments.FinancialErrors.Invalid("paymentInstructionRegeneration").ConvertError<Guid>();
        }

        var booking = await queryService.GetBookingById(current.BookingId, ct);
        if (booking is null)
        {
            return await RecordAndReturn(
                BookingErrors.BookingNotFound().ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.BookingNotFound,
                ct);
        }

        var tourAggregate = await tourStore.GetByBookingId(booking.Id, ct);
        if (tourAggregate is null)
        {
            return await RecordAndReturn(
                BookingErrors.BookingNotFound().ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.BookingNotFound,
                ct);
        }

        var eligibility = tourAggregate.CanReceiveCustomerDocuments(booking.Id);
        if (eligibility.IsFailure)
        {
            return await RecordAndReturn(
                eligibility.ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.StateConflict,
                ct);
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            return await RecordAndReturn(
                TourErrors.TourNotFound().ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.TourNotFound,
                ct);
        }

        var brandingResult = await documentBrandingSource.Capture(ct);
        if (brandingResult.IsFailure)
        {
            return await RecordAndReturn(
                brandingResult.ConvertError<DocumentBrandingSnapshotValues, Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.BrandingUnavailable,
                ct);
        }

        var branding = brandingResult.Value;
        var contentResult = ContractDocumentDraftFactory.Create(
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding);
        if (contentResult.IsFailure)
        {
            return await RecordAndReturn(
                contentResult.ConvertError<DocumentDraftContent, Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.ValidationRejected,
                ct);
        }

        return await commit(
            async () =>
            {
                var replacementResult = lineage.CreateRevision(current.Id, contentResult.Value, now, command.AuditContext);
                if (replacementResult.IsFailure)
                {
                    return await RecordAndReturn(
                        replacementResult.ConvertError<DocumentDraft, Guid>(),
                        command.AuditContext,
                        current.Id,
                        current.BookingId,
                        current.Revision,
                        DocumentAuditReasonCode.ValidationRejected,
                        ct);
                }

                return Result.Ok(replacementResult.Value.Id);
            },
            lineage.Id,
            current.Id,
            current.BookingId,
            current.Revision);
    }

    private async Task<Result<Guid>> RecordAndReturn(
        Result<Guid> operationResult,
        DocumentAuditContext auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var auditResult = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Regenerate,
            documentId,
            bookingId,
            documentRevision,
            DocumentAuditOutcome.Rejected,
            reasonCode,
            ct);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError<Guid>();
        }

        return operationResult;
    }
}
