using SharedKernel.Results;
using SharedKernel.Branding;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Creates a new document revision without mutating prior artifacts.</summary>
public sealed class RegenerateDocumentDraftCommandHandler(
    IDocumentStore documentStore,
    IQueryService queryService,
    IBrandingApiClient brandingApiClient,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    DocumentAuditWriter documentAuditWriter)
{
    /// <summary>Generates and persists the replacement revision.</summary>
    public async Task<Result<Guid>> Handle(RegenerateDocumentDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Guid>();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lineage = await documentStore.GetByDocumentId(command.DocumentId, ct);
        var current = lineage?.GetRevision(command.DocumentId);
        if (lineage is null || current is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound(command.DocumentId).ConvertError<Guid>(),
                command.AuditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                ct);
        }

        var booking = await queryService.GetBookingById(current.BookingId, ct);
        if (booking is null)
        {
            return await RecordAndReturn(
                BookingErrors.BookingNotFound(current.BookingId).ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.BookingNotFound,
                ct);
        }

        if (!BookingMapper.MapToBookingStatus(booking.Status).IsAccepted())
        {
            return await RecordAndReturn(
                DocumentErrors.BookingIsNotAccepted().ConvertError<Guid>(),
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
                TourErrors.TourNotFound(booking.TourId).ConvertError<Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.TourNotFound,
                ct);
        }

        var branding = await DocumentBrandingSnapshotFactory.Capture(brandingApiClient, ct);
        var contentResult = ContractDocumentDraftFactory.Create(
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding,
            now);
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

        await unitOfWork.SaveEntities(ct);
        return Result.Ok(replacementResult.Value.Id);
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
