namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Captures the aggregate discovery counts emitted in the initial report.
/// </summary>
internal sealed class DiscoveryCounts(int requestCount, int handlerCount, int pipelineCount)
{
    /// <summary>
    /// Gets the number of discovered request contracts.
    /// </summary>
    public int RequestCount { get; } = requestCount;

    /// <summary>
    /// Gets the number of discovered request handlers.
    /// </summary>
    public int HandlerCount { get; } = handlerCount;

    /// <summary>
    /// Gets the number of discovered pipeline behaviors.
    /// </summary>
    public int PipelineCount { get; } = pipelineCount;
}
