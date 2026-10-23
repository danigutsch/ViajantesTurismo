using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit ApprovePaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class ApprovePaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow)
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public Task<Result<Guid>> Handle(ApprovePaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.Change(command.DocumentId, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.Approved, ct);
    }
}
