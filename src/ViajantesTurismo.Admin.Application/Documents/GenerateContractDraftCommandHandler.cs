using SharedKernel.Results;
using SharedKernel.Branding;
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
    TimeProvider timeProvider)
{
    /// <summary>Generates and persists a new draft revision.</summary>
    public async Task<Result<Guid>> Handle(GenerateContractDraftCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var booking = await queryService.GetBookingById(command.BookingId, ct);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound(command.BookingId).ConvertError<Guid>();
        }

        var tour = await queryService.GetTourById(booking.TourId, ct);
        if (tour is null)
        {
            return TourErrors.TourNotFound(booking.TourId).ConvertError<Guid>();
        }

        var branding = await DocumentBrandingSnapshotFactory.Capture(brandingApiClient, ct);
        var draftResult = ContractDocumentDraftFactory.Create(
            booking,
            tour,
            command.TemplateId,
            command.TemplateVersion,
            branding,
            timeProvider.GetUtcNow().UtcDateTime);
        if (draftResult.IsFailure)
        {
            return draftResult.ConvertError<DocumentDraft, Guid>();
        }

        documentStore.Add(draftResult.Value);
        await unitOfWork.SaveEntities(ct);
        return Result.Ok(draftResult.Value.Id);
    }
}
