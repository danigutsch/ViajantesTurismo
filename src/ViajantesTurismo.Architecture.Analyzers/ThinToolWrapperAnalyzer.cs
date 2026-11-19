using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViajantesTurismo.Architecture.Analyzers;

/// <summary>Reports direct HTTP requests within repository tool wrappers.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThinToolWrapperAnalyzer : DiagnosticAnalyzer
{
    private const string ToolWrapperProperty = "build_property.ViajantesTurismoToolWrapper";

    private static readonly DiagnosticDescriptor ToolWrapperHttpRule = new(
        ArchitectureDiagnosticIds.ToolWrapperHttp,
        title: "Tool wrappers delegate HTTP requests to source clients",
        messageFormat: "Move the HTTP request '{0}' into a client in src and call that client from the tool wrapper",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Tool wrappers compose reusable clients. HTTP request execution belongs in a source library.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [ToolWrapperHttpRule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var options = compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            if (!options.TryGetValue(ToolWrapperProperty, out var value)
                || !string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var httpClient = compilationContext.Compilation.GetTypeByMetadataName("System.Net.Http.HttpClient");
            var invoker = compilationContext.Compilation.GetTypeByMetadataName("System.Net.Http.HttpMessageInvoker");
            var jsonExtensions = compilationContext.Compilation.GetTypeByMetadataName("System.Net.Http.Json.HttpClientJsonExtensions");
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, httpClient, invoker, jsonExtensions),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol? httpClient,
        INamedTypeSymbol? invoker,
        INamedTypeSymbol? jsonExtensions)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        while (method.OverriddenMethod is { } overridden)
        {
            method = overridden;
        }

        var type = method.ContainingType;
        var symbols = SymbolEqualityComparer.Default;
        var isClientRequest = symbols.Equals(type, httpClient)
            && method.Name is "GetAsync" or "GetByteArrayAsync" or "GetStreamAsync" or "GetStringAsync"
                or "PostAsync" or "PutAsync" or "PatchAsync" or "DeleteAsync" or "Send" or "SendAsync";
        var isInvokerRequest = symbols.Equals(type, invoker) && method.Name is "Send" or "SendAsync";
        var isJsonRequest = method.IsExtensionMethod && symbols.Equals(type, jsonExtensions);
        if (isClientRequest || isInvokerRequest || isJsonRequest)
        {
            context.ReportDiagnostic(Diagnostic.Create(ToolWrapperHttpRule, invocation.Syntax.GetLocation(), method.Name));
        }
    }
}
