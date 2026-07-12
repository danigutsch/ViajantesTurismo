using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Documents;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Moves an eligible draft into staff review.</summary>
public sealed class BeginDocumentReviewCommandHandler(
    IDocumentStore documentStore,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Starts or resumes review and persists the status transition.</summary>
    public async Task<Result> Handle(BeginDocumentReviewCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var document = await documentStore.GetById(command.DocumentId, ct);
        if (document is null)
        {
            return DocumentErrors.DocumentNotFound(command.DocumentId);
        }

        var result = document.BeginReview(timeProvider.GetUtcNow().UtcDateTime);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveEntities(ct);
        return Result.Ok();
    }
}
