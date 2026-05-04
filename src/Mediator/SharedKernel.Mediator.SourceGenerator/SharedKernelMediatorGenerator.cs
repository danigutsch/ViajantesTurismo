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
        var generatorOptions = context.AnalyzerConfigOptionsProvider.Select(
            static (optionsProvider, _) => MediatorGeneratorConfigOptions.Parse(optionsProvider));
        var generationInput = discoveryModel.Combine(generatorOptions);

        context.RegisterSourceOutput(
            generationInput,
            static (productionContext, input) =>
            {
                var (discoveryModel, generatorOptions) = input;

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

                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.AppMediator.g.cs",
                    AppMediatorEmitter.Emit(discoveryModel));

                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.GeneratedDispatch.g.cs",
                    GeneratedDispatchEmitter.Emit(discoveryModel));

                productionContext.AddSource(
                    "SharedKernel.Mediator.Generated.GeneratedPipelines.g.cs",
                    GeneratedPipelinesEmitter.Emit(discoveryModel));

                if (generatorOptions.EmitCallGraphJson)
                {
                    productionContext.AddSource(
                        "SharedKernel.Mediator.Generated.CallGraph.g.cs",
                        CallGraphArtifactEmitter.Emit(discoveryModel));
                }
            });
    }
}
