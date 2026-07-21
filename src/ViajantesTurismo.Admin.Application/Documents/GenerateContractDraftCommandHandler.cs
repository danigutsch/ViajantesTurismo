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
    DocumentAuditWriter documentAuditWriter)
{
    /// <summary>Generates and persists a new draft revision.</summary>
    public async Task<Result<Guid>> Handle(GenerateContractDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.AuditContext is null)
        {
            return DocumentAuditErrors.AuditContextRequired().ConvertError<Guid>();
        }

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
                null,
                booking.Id,
                null,
                DocumentAuditReasonCode.ValidationRejected,
                ct);
        }

        var lineageResult = DocumentLineage.Create(
            booking.Id,
            DocumentType.BookingConfirmationContract,
            DocumentAudience.Customer,
            contentResult.Value,
            now,
            command.AuditContext);
        if (lineageResult.IsFailure)
        {
            return await RecordAndReturn(
                lineageResult.ConvertError<DocumentLineage, Guid>(),
                command.AuditContext,
                null,
                booking.Id,
                null,
                DocumentAuditReasonCode.ValidationRejected,
                ct);
        }

        var lineage = lineageResult.Value;
        var draft = lineage.Revisions[0];
        documentStore.Add(lineage);
        await unitOfWork.SaveEntities(ct);
        return Result.Ok(draft.Id);
    }

    private async Task<Result<Guid>> RecordAndReturn(
        Result<Guid> operationResult,
        DocumentAuditContext? auditContext,
        Guid? documentId,
        Guid? bookingId,
        int? documentRevision,
        DocumentAuditReasonCode reasonCode,
        CancellationToken ct)
    {
        var auditResult = await documentAuditWriter.Add(
            auditContext,
            DocumentAuditOperation.Generate,
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
