using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Accumulates raw discovery results before descriptor projection.
/// </summary>
internal sealed class DiscoveryState
{
    public Dictionary<string, RawRequestContract> RequestContracts { get; } = new(StringComparer.Ordinal);

    public List<HandlerDescriptor> RequestHandlers { get; } = [];

    public List<PipelineDescriptor> Pipelines { get; } = [];

    public Dictionary<string, string> NotificationContracts { get; } = new(StringComparer.Ordinal);

    public List<NotificationHandlerDescriptor> NotificationHandlers { get; } = [];

    public Dictionary<string, ResponseDescriptor> StreamRequestContracts { get; } = new(StringComparer.Ordinal);

    public List<StreamHandlerDescriptor> StreamHandlers { get; } = [];

    public List<Diagnostic> Diagnostics { get; } = [];

    public HashSet<string> DiagnosedInaccessibleTypes { get; } = new(StringComparer.Ordinal);

    public HashSet<string> GeneratedRegistrationKeys { get; } = new(StringComparer.Ordinal);

    public HashSet<string> DiagnosedDuplicateRegistrationKeys { get; } = new(StringComparer.Ordinal);

    public HashSet<string> DiagnosedInvalidHandlerKeys { get; } = new(StringComparer.Ordinal);
}
