using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a request handler discovered for a request contract.
/// </summary>
internal sealed record HandlerDescriptor(
    string MetadataName,
    string Namespace,
    string Name,
    string RequestMetadataName,
    string ResponseMetadataName,
    string MethodName,
    Accessibility Accessibility,
    HandlerKind Kind);
