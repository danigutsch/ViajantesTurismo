using SharedKernel.Mediator;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit WithdrawPaymentInstructionApproval instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class WithdrawPaymentInstructionApprovalCommandHandler(PaymentInstructionWorkflow workflow) : ICommandHandler<WithdrawPaymentInstructionApprovalCommand, Result<Guid>>
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public ValueTask<Result<Guid>> Handle(WithdrawPaymentInstructionApprovalCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.ApplyChange(command.Target, command.OperationScope, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.ApprovalWithdrawn, ct);
    }
}
