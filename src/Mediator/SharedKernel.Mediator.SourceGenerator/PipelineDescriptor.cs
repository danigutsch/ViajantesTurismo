namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Describes a pipeline behavior discovered for a request contract.
/// </summary>
internal sealed record PipelineDescriptor(
    string MetadataName,
    string? OpenGenericMetadataName,
    string RequestMetadataName,
    int Stage,
    int Order,
    PipelineApplicability Applicability,
    bool IsAccessibleToGeneratedMediator);
