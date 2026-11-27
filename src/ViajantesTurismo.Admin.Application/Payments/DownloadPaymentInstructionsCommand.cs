using SharedKernel.BuildingBlocks;
using SharedKernel.Mediator;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests a finalized artifact after locking and revalidating its payment sources.</summary>
public sealed record DownloadPaymentInstructionsCommand(PaymentInstructionTarget Target, string ActorId)
    : RedactedDisplayRecord, ICommand<Result<FinalizedDocumentArtifact>>, IPaymentCommand
{
    /// <inheritdoc />
    public IReadOnlyList<string> TransactionLocks => [PaymentCommandLocks.Booking(Target.BookingId), PaymentCommandLocks.Account(Target.AccountId)];
}
