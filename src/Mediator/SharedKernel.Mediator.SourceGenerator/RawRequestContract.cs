namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Captures raw request data before handlers and pipelines are attached.
/// </summary>
internal sealed record RawRequestContract(
    string MetadataName,
    string Namespace,
    string Name,
    RequestKind Kind,
    ResponseDescriptor Response,
    bool IsValueType);
