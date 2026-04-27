using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Accumulates raw discovery results before descriptor projection.
/// </summary>
internal sealed class DiscoveryState
{
    public Dictionary<string, RawRequestContract> RequestContracts { get; } = [with(StringComparer.Ordinal)];

    public List<HandlerDescriptor> RequestHandlers { get; } = [];

    public List<PipelineDescriptor> Pipelines { get; } = [];

    public Dictionary<string, string> NotificationContracts { get; } = [with(StringComparer.Ordinal)];

    public List<NotificationHandlerDescriptor> NotificationHandlers { get; } = [];

    public Dictionary<string, ResponseDescriptor> StreamRequestContracts { get; } = [with(StringComparer.Ordinal)];

    public List<StreamHandlerDescriptor> StreamHandlers { get; } = [];

    public List<Diagnostic> Diagnostics { get; } = [];

    public HashSet<string> DiagnosedInaccessibleTypes { get; } = [with(StringComparer.Ordinal)];
}
