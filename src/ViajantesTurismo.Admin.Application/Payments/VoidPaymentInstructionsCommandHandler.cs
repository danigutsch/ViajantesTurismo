using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit VoidPaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class VoidPaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow) : ICommandHandler<VoidPaymentInstructionsCommand, Result<Guid>>
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public ValueTask<Result<Guid>> Handle(VoidPaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.ApplyChange(command.Target, command.OperationScope, command.Input, command.ActorId, command.IdempotencyKey, PaymentInstructionAuditOperation.Voided, ct);
    }
}
