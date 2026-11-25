using SharedKernel.Mediator;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit FinalizePaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class FinalizePaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow) : ICommandHandler<FinalizePaymentInstructionsCommand, Result<Guid>>
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public ValueTask<Result<Guid>> Handle(FinalizePaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.ApplyChange(command.Target, command.OperationScope, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.Finalized, ct);
    }
}
