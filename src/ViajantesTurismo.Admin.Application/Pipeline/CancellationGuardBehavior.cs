using PandaBear.Mediator;

namespace ViajantesTurismo.Admin.Application.Pipeline;

/// <summary>
/// Rejects a command before any handler work when the caller has already cancelled the request.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <typeparam name="TResponse">The command response type.</typeparam>
[PipelineOrder(PipelineStage.Validation, Order = 0)]
public sealed class CancellationGuardBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    /// <inheritdoc />
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        ct.ThrowIfCancellationRequested();

        return next();
    }
}
