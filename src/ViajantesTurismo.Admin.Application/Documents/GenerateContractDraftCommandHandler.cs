using SharedKernel.Results;
using SharedKernel.Branding;
using ViajantesTurismo.Admin.Application.Mappings;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>
/// Generates a classified customer-facing booking confirmation contract draft.
/// </summary>
public sealed class GenerateContractDraftCommandHandler(
    IQueryService queryService,
    IDocumentStore documentStore,
    IBrandingApiClient brandingApiClient,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentAuditStore? auditStore = null)
{
    /// <summary>Generates and persists a new draft revision.</summary>
    public async Task<Result<Guid>> Handle(GenerateContractDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var booking = await queryService.GetBookingById(command.BookingId, ct);
        if (booking is null)
        {
            return await RecordAndReturn(
                BookingErrors.BookingNotFound(command.BookingId).ConvertError<Guid>(),
                command.AuditContext,
                null,
                command.BookingId,
                null,
                DocumentAuditReasonCode.BookingNotFound,
                now,
                ct);
        }

        if (!BookingMapper.MapToBookingStatus(booking.Status).IsAccepted())
        {
            return await RecordAndReturn(
                DocumentErrors.BookingIsNotAccepted().ConvertError<Guid>(),
                command.AuditContext,
                null,
                booking.Id,
                null,
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
                null,
                booking.Id,
                null,
                DocumentAuditReasonCode.TourNotFound,
                now,
                ct);
        }

        var branding = await DocumentBrandingSnapshotFactory.Capture(brandingApiClient, ct);
        var draftResult = ContractDocumentDraftFactory.Create(
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding,
            now);
        if (draftResult.IsFailure)
        {
            return await RecordAndReturn(
                draftResult.ConvertError<DocumentDraft, Guid>(),
                command.AuditContext,
                null,
                booking.Id,
                null,
                DocumentAuditReasonCode.ValidationRejected,
                now,
                ct);
        }

        documentStore.Add(draftResult.Value);
        var auditResult = DocumentAuditWriter.Add(
            auditStore,
            command.AuditContext,
            DocumentAuditOperation.Generate,
            draftResult.Value.Id,
            draftResult.Value.BookingId,
            draftResult.Value.Revision,
            DocumentAuditOutcome.Succeeded,
            DocumentAuditReasonCode.ManualOperation,
            now);
        if (auditResult.IsFailure)
        {
            return auditResult.ConvertError<bool, Guid>();
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok(draftResult.Value.Id);
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
            DocumentAuditOperation.Generate,
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
