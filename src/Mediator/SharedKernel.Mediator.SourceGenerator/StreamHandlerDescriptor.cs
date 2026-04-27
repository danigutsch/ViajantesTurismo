using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a handler discovered for a stream request contract.
/// </summary>
internal sealed record StreamHandlerDescriptor(
    string MetadataName,
    string Namespace,
    string Name,
    string RequestMetadataName,
    string ResponseMetadataName,
    string MethodName,
    Accessibility Accessibility,
    bool IsAccessibleToGeneratedMediator);
