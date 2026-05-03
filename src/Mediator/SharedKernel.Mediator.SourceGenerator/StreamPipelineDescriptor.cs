namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a stream pipeline behavior discovered for a stream request contract.
/// </summary>
internal sealed record StreamPipelineDescriptor(
    string MetadataName,
    string? OpenGenericMetadataName,
    string RequestMetadataName,
    int Stage,
    int Order,
    PipelineApplicability Applicability,
    bool IsAccessibleToGeneratedMediator);
