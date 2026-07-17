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
    IDocumentAuditStore? auditStore = null)
{
    /// <summary>Generates and persists the replacement revision.</summary>
    public async Task<Result<Guid>> Handle(RegenerateDocumentDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var current = await documentStore.GetById(command.DocumentId, ct);
        if (current is null)
        {
            return await RecordAndReturn(
                DocumentErrors.DocumentNotFound(command.DocumentId).ConvertError<Guid>(),
                command.AuditContext,
                command.DocumentId,
                null,
                null,
                DocumentAuditReasonCode.DocumentNotFound,
                now,
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
                now,
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
                now,
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
                now,
                ct);
        }

        var branding = await DocumentBrandingSnapshotFactory.Capture(brandingApiClient, ct);
        var replacementResult = ContractDocumentDraftFactory.CreateRevision(
            current,
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding,
            now);
        if (replacementResult.IsFailure)
        {
            return await RecordAndReturn(
                replacementResult.ConvertError<DocumentDraft, Guid>(),
                command.AuditContext,
                current.Id,
                current.BookingId,
                current.Revision,
                DocumentAuditReasonCode.ValidationRejected,
                now,
                ct);
        }

        documentStore.Add(replacementResult.Value);
        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            command.AuditContext,
            DocumentAuditOperation.Regenerate,
            replacementResult.Value.Id,
            replacementResult.Value.BookingId,
            replacementResult.Value.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualRegeneration,
            now);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError<bool, Guid>();
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok(replacementResult.Value.Id);
    }

    private async Task<Result<Guid>> RecordAndReturn(
        Result<Guid> operationResult,
        DocumentAuditContext? auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        DateTime occurredAtUtc,
        CancellationToken ct)
    {
        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            auditContext,
            DocumentAuditOperation.Regenerate,
            documentId,
            bookingId,
            documentRevision,
            DocumentAuditOutcome.Rejected,
            reasonCode,
            occurredAtUtc);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError<bool, Guid>();
        }

        if (auditResult.Value)
        {
            await unitOfWork.SaveEntities(ct);
        }

        return operationResult;
    }
}
