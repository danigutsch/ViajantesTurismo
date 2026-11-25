using SharedKernel.Mediator.EntityFrameworkCore;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents;
using ViajantesTurismo.Admin.Application.Payments;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Persists an authenticated instruction refusal after the original transaction rolls back.</summary>
internal sealed class PaymentInstructionDownloadRollbackAudit(IPaymentInstructionStore instructions, IUnitOfWork unitOfWork, TimeProvider clock)
    : IAfterRollback<DownloadPaymentInstructionsCommand, Result<FinalizedDocumentArtifact>>
{
    /// <inheritdoc />
    public async ValueTask Handle(DownloadPaymentInstructionsCommand request, Result<FinalizedDocumentArtifact> response, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ActorId))
        {
            return;
        }

        var target = request.Target;
        instructions.AddAudit(target.BookingId, new PaymentInstructionAuditMetadata(target.DocumentId, target.Revision,
            request.ActorId, clock.GetUtcNow(), PaymentInstructionAuditOperation.DownloadRejected, FinancialReason.InstructionDownloadRejected, false));
        await unitOfWork.SaveEntities(ct);
    }
}
