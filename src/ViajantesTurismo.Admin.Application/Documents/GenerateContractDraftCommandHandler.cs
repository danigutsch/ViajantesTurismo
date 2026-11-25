using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Generates a classified customer-facing booking confirmation contract draft.
/// </summary>
public sealed class GenerateContractDraftCommandHandler(
    IQueryService queryService,
    ITourStore tourStore,
    IDocumentStore documentStore,
    DocumentBrandingSource documentBrandingSource,
    TimeProvider timeProvider,
    DocumentCommandIdempotency documentCommandIdempotency)
    : ICommandHandler<GenerateContractDraftCommand, Result<Guid>>
{
    /// <summary>Generates and persists a new draft revision.</summary>
    public ValueTask<Result<Guid>> Handle(GenerateContractDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new(documentCommandIdempotency.Execute(command, commit => Prepare(command, commit, ct), ct));
    }

    private async Task<Result<Guid>> Prepare(
        GenerateContractDraftCommand command,
        DocumentCommandCommit commit,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var booking = await queryService.GetBookingById(command.BookingId, ct);
        if (booking is null)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.BookingNotFound, BookingId: command.BookingId));
            return BookingErrors.BookingNotFound().ConvertError<Guid>();
        }

        var tourAggregate = await tourStore.GetByBookingId(booking.Id, ct);
        if (tourAggregate is null)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.BookingNotFound, BookingId: booking.Id));
            return BookingErrors.BookingNotFound().ConvertError<Guid>();
        }

        var eligibility = tourAggregate.CanReceiveCustomerDocuments(booking.Id);
        if (eligibility.IsFailure)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.StateConflict, BookingId: booking.Id));
            return eligibility.ConvertError<Guid>();
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.TourNotFound, BookingId: booking.Id));
            return TourErrors.TourNotFound().ConvertError<Guid>();
        }

        var brandingResult = await documentBrandingSource.Capture(ct);
        if (brandingResult.IsFailure)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.BrandingUnavailable, BookingId: booking.Id));
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
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.ValidationRejected, BookingId: booking.Id));
            return contentResult.ConvertError<DocumentDraftContent, Guid>();
        }

        var lineageResult = DocumentLineage.Create(
            booking.Id,
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            contentResult.Value,
            now,
            command.AuditContext,
            command.LineageId);
        if (lineageResult.IsFailure)
        {
            command.DeferRejectionAudit(new DocumentRejectionAudit(DocumentAuditReasonCode.ValidationRejected, BookingId: booking.Id));
            return lineageResult.ConvertError<DocumentLineage, Guid>();
        }

        var lineage = lineageResult.Value;
        var draft = lineage.Revisions[0];
        return await commit(
            () =>
            {
                documentStore.Add(lineage);
                return Task.FromResult(Result.Ok(draft.Id));
            },
            lineage.Id,
            null,
            command.BookingId,
            null);
    }
}
