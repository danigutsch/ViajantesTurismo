using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Executes the explicit ReviewPaymentInstructions instruction action with concurrency preconditions and durable idempotency.</summary>
public sealed class ReviewPaymentInstructionsCommandHandler(PaymentInstructionWorkflow workflow)
{
    /// <summary>Applies the requested instruction lifecycle action.</summary>
    public Task<Result<Guid>> Handle(ReviewPaymentInstructionsCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return workflow.Review(command.DocumentId, command.Input, command.ActorId, command.IdempotencyKey, ct);
    }
}
