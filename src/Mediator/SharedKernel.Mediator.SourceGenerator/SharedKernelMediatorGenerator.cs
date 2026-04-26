using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Discovers mediator contracts and emits an initial readable discovery report.
/// </summary>
[Generator]
public sealed class SharedKernelMediatorGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var discoveryCounts = context.CompilationProvider.Select(
            static (compilation, cancellationToken) => DiscoveryModelBuilder.Build(compilation, cancellationToken));

        context.RegisterSourceOutput(
            discoveryCounts,
            static (productionContext, counts) =>
            {
                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.DiscoveryReport.g.cs",
                    DiscoveryReportEmitter.Emit(counts));
            });
    }
}
