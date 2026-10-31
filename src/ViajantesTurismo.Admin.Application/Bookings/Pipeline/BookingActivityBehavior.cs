using PandaBear.Mediator;

namespace ViajantesTurismo.Admin.Application.Bookings.Pipeline;

/// <summary>
/// Binds the runtime <see cref="ActivityBehavior{TRequest, TResponse}"/> into the Bookings command pipeline,
/// because the generator only discovers behaviors declared in marked or primary assemblies.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <typeparam name="TResponse">The command response type.</typeparam>
[PipelineOrder(PipelineStage.Observability, Order = 0)]
public sealed class BookingActivityBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly ActivityBehavior<TRequest, TResponse> inner = new();

    /// <inheritdoc />
    public ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
        => inner.Handle(request, next, ct);
}
