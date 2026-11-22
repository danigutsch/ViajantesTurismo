using PandaBear.Mediator;
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
    DocumentCommandIdempotency documentCommandIdempotency)
    : ICommandHandler<RegenerateDocumentDraftCommand, Result<Guid>>
{
    /// <summary>Generates and persists the replacement revision.</summary>
    public ValueTask<Result<Guid>> Handle(RegenerateDocumentDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new(documentCommandIdempotency.Execute(command, commit => Prepare(command, commit, ct), ct));
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
            command.DeferRejectionAudit(DocumentAuditReasonCode.DocumentNotFound);
            return DocumentErrors.DocumentNotFound().ConvertError<Guid>();
        }

        if (lineage.RequiresPaymentInstructionWorkflow)
        {
            return Domain.Payments.FinancialErrors.Invalid("paymentInstructionRegeneration").ConvertError<Guid>();
        }

        var booking = await queryService.GetBookingById(current.BookingId, ct);
        if (booking is null)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.BookingNotFound, current);
            return BookingErrors.BookingNotFound().ConvertError<Guid>();
        }

        var tourAggregate = await tourStore.GetByBookingId(booking.Id, ct);
        if (tourAggregate is null)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.BookingNotFound, current);
            return BookingErrors.BookingNotFound().ConvertError<Guid>();
        }

        var eligibility = tourAggregate.CanReceiveCustomerDocuments(booking.Id);
        if (eligibility.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.StateConflict, current);
            return eligibility.ConvertError<Guid>();
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.TourNotFound, current);
            return TourErrors.TourNotFound().ConvertError<Guid>();
        }

        var brandingResult = await documentBrandingSource.Capture(ct);
        if (brandingResult.IsFailure)
        {
            command.DeferRejectionAudit(DocumentAuditReasonCode.BrandingUnavailable, current);
            return brandingResult.ConvertError<DocumentBrandingSnapshotValues, Guid>();
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
            command.DeferRejectionAudit(DocumentAuditReasonCode.ValidationRejected, current);
            return contentResult.ConvertError<DocumentDraftContent, Guid>();
        }

        return await commit(
            () =>
            {
                var replacementResult = lineage.CreateRevision(current.Id, contentResult.Value, now, command.AuditContext);
                if (replacementResult.IsFailure)
                {
                    command.DeferRejectionAudit(DocumentAuditReasonCode.ValidationRejected, current);
                    return Task.FromResult(replacementResult.ConvertError<DocumentDraft, Guid>());
                }

                return Task.FromResult(Result.Ok(replacementResult.Value.Id));
            },
            lineage.Id,
            current.Id,
            current.BookingId,
            current.Revision);
    }
}
