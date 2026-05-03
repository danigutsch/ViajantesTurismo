using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.Analyzers;

/// <summary>
/// Declares the diagnostics emitted by the mediator analyzer package.
/// </summary>
internal static class MediatorAnalyzerDescriptors
{
    public static readonly DiagnosticDescriptor InvalidHandlerSignature = new(
        id: MediatorDiagnosticIds.InvalidHandlerSignature,
        title: "Handler has invalid signature",
        messageFormat: "Handler '{0}' does not expose a compatible public Handle method for request '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingCancellationToken = new(
        id: MediatorDiagnosticIds.MissingCancellationToken,
        title: "Handler is missing CancellationToken ct",
        messageFormat: "Handler '{0}' does not expose a public Handle method that accepts CancellationToken ct for request '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HandlerReturnTypeMismatch = new(
        id: MediatorDiagnosticIds.HandlerReturnTypeMismatch,
        title: "Handler return type does not match request response type",
        messageFormat: "Handler '{0}' returns '{1}' but request '{2}' requires '{3}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingCancellationForwarding = new(
        id: MediatorDiagnosticIds.MissingCancellationForwarding,
        title: "Mediator call does not pass available CancellationToken ct",
        messageFormat: "Call '{0}' should forward available CancellationToken '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingEnumeratorCancellation = new(
        id: MediatorDiagnosticIds.MissingEnumeratorCancellation,
        title: "Async stream Handle method is missing [EnumeratorCancellation]",
        messageFormat: "Async stream Handle method '{0}' should annotate CancellationToken '{1}' with [EnumeratorCancellation]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidPipelineGenericArity = new(
        id: MediatorDiagnosticIds.InvalidPipelineGenericArity,
        title: "Pipeline behavior has invalid generic arity",
        messageFormat: "Pipeline behavior '{0}' declares {1} type parameter(s); open generic pipeline behaviors must declare exactly two type parameters",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HandlerShouldNotCallSender = new(
        id: MediatorDiagnosticIds.HandlerShouldNotCallSender,
        title: "Handler should not call ISender.Send",
        messageFormat: "Handler '{0}' should not call mediator send APIs directly",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}
