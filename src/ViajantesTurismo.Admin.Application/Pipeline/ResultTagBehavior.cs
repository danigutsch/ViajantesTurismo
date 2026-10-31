using System.Diagnostics;
using PandaBear.Mediator;
using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Pipeline;

/// <summary>
/// Tags the current request activity with <c>mediator.result</c> (<c>ok</c> or <c>failed</c>) so a failed
/// <see cref="Result"/>, which the mediator runtime counts as a successful dispatch, stays visible in traces.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
/// <typeparam name="TResponse">The command response type.</typeparam>
[PipelineOrder(PipelineStage.Observability, Order = 1)]
public sealed class ResultTagBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : struct, IResultLike<TResponse>
{
    /// <summary>The span attribute name.</summary>
    public const string TagName = "mediator.result";

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerContinuation<TResponse> next, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        var response = await next().ConfigureAwait(false);
        Activity.Current?.SetTag(TagName, response.IsSuccess ? "ok" : "failed");
        return response;
    }
}
