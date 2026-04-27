using System.Collections.Immutable;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a discovered stream request contract.
/// </summary>
internal sealed record StreamRequestDescriptor(
    string MetadataName,
    ResponseDescriptor ItemResponse,
    ImmutableArray<StreamHandlerDescriptor> Handlers);
