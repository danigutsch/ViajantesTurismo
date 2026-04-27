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
        var discoveryModel = context.CompilationProvider.Select(
            static (compilation, cancellationToken) => DiscoveryModelBuilder.Build(compilation, cancellationToken));

        context.RegisterSourceOutput(
            discoveryModel,
            static (productionContext, discoveryModel) =>
            {
                foreach (var diagnostic in discoveryModel.Diagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);
                }

                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.DiscoveryReport.g.cs",
                    DiscoveryReportEmitter.Emit(discoveryModel));

                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.DependencyInjection.g.cs",
                    DependencyInjectionEmitter.Emit(discoveryModel));
            });
    }
}
