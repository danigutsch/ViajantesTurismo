namespace SharedKernel.Mediator;

/// <summary>
/// Sends request messages to their generated handlers.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request through the generated dispatch path.
    /// </summary>
    /// <typeparam name="TResponse">The response type produced by the request.</typeparam>
    /// <param name="request">The request instance to send.</param>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>The produced response value.</returns>
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct);
}
