using SharedKernel.Results;
using SharedKernel.Branding;
using ViajantesTurismo.Admin.Domain.Documents;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Creates a new document revision without mutating prior artifacts.</summary>
public sealed class RegenerateDocumentDraftCommandHandler(
    IDocumentStore documentStore,
    IQueryService queryService,
    IBrandingApiClient brandingApiClient,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Generates and persists the replacement revision.</summary>
    public async Task<Result<Guid>> Handle(RegenerateDocumentDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var current = await documentStore.GetById(command.DocumentId, ct);
        if (current is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId).ConvertError<Guid>();
        }

        var booking = await queryService.GetBookingById(current.BookingId, ct);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound(current.BookingId).ConvertError<Guid>();
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(booking.TourId).ConvertError<Guid>();
        }

        var branding = await DocumentBrandingSnapshotFactory.Capture(brandingApiClient, ct);
        var replacementResult = ContractDocumentDraftFactory.CreateRevision(
            current,
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding,
            timeProvider.GetUtcNow().UtcDateTime);
        if (replacementResult.IsFailure)
        {
            return replacementResult.ConvertError<DocumentDraft, Guid>();
        }

        documentStore.Add(replacementResult.Value);
        await unitOfWork.SaveEntities(ct);
        return Result.Ok(replacementResult.Value.Id);
    }
}
