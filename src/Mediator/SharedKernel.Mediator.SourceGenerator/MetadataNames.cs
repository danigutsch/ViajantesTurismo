namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Provides metadata names for the abstractions used during discovery.
/// </summary>
internal static class MetadataNames
{
    public const string Request = "SharedKernel.Mediator.IRequest`1";
    public const string RequestHandler = "SharedKernel.Mediator.IRequestHandler`2";
    public const string PipelineBehavior = "SharedKernel.Mediator.IPipelineBehavior`2";
    public const string MediatorModuleAttribute = "SharedKernel.Mediator.MediatorModuleAttribute";
}
