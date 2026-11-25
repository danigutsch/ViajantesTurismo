using SharedKernel.Mediator;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit ReviewPaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class ReviewPaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow) : ICommandHandler<ReviewPaymentInstructionsCommand, Result<Guid>>
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public ValueTask<Result<Guid>> Handle(ReviewPaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.ApplyReview(command.Target, command.OperationScope, command.Input, command.ActorId, command.IdempotencyKey, ct);
    }
}
