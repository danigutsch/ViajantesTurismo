using PandaBear.Mediator;
using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Documents;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Checks and audits an instruction download within the payment transaction.</summary>
public sealed class DownloadPaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow)
    : ICommandHandler<DownloadPaymentInstructionsCommand, Result<FinalizedDocumentArtifact>>
{
    /// <inheritdoc />
    public ValueTask<Result<FinalizedDocumentArtifact>> Handle(DownloadPaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.ReadArtifact(command.Target, command.ActorId, ct);
    }
}
