using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit WithdrawPaymentInstructionApproval instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class WithdrawPaymentInstructionApprovalCommandHandler(PaymentInstructionWorkflow workflow)
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public Task<Result<Guid>> Handle(WithdrawPaymentInstructionApprovalCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.Change(command.DocumentId, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.ApprovalWithdrawn, ct);
    }
}
