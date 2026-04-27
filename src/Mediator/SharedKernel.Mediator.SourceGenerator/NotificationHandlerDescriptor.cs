using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a handler discovered for a notification contract.
/// </summary>
internal sealed record NotificationHandlerDescriptor(
    string MetadataName,
    string Namespace,
    string Name,
    string NotificationMetadataName,
    string MethodName,
    Accessibility Accessibility,
    bool IsAccessibleToGeneratedMediator);
