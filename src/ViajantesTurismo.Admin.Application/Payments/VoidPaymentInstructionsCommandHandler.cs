using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit VoidPaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class VoidPaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow)
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public Task<Result<Guid>> Handle(VoidPaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.Change(command.DocumentId, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.Voided, ct);
    }
}
