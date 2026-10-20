using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViajantesTurismo.Architecture.Analyzers.Tests;

internal static class AnalyzerTestHarness
{
    public const string AdminApiAssemblyName = "ViajantesTurismo.Admin.ApiService";
    public const string BrandingApiAssemblyName = "ViajantesTurismo.Branding.ApiService";
    public const string CatalogApiAssemblyName = "ViajantesTurismo.Catalog.ApiService";

    private const string DefaultUsings = """
        using System;
        using System.Net.Http;
        using System.Threading.Tasks;
        using Demo;
        using ServiceCollection = Demo.ServiceCollection;
        using Microsoft.Extensions.DependencyInjection;
        using SharedKernel.Branding;
        using ViajantesTurismo.Resources;

        """;

    public static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName = AdminApiAssemblyName,
        string path = "TestSource.cs")
    {
        var compilation = SharedKernel.Testing.Roslyn.AnalyzerTestHarness.CreateCompilation(
            source,
            DefaultUsings,
            assemblyName: assemblyName,
            path: path);
        var supportTree = CSharpSyntaxTree.ParseText(
            AnalyzerTestSupport.Source,
            new CSharpParseOptions(LanguageVersion.Preview),
            path: "AnalyzerTestSupport.cs");
        var compilationWithSupport = compilation.AddSyntaxTrees(supportTree);
        var compilerErrors = compilationWithSupport.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilerErrors.Length > 0)
        {
            throw new InvalidOperationException(
                $"Analyzer test source did not compile:{Environment.NewLine}"
                + string.Join(Environment.NewLine, compilerErrors.Select(static diagnostic => diagnostic.ToString())));
        }

        return compilationWithSupport;
    }

    public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnostics(
        string source,
        string assemblyName = AdminApiAssemblyName,
        string path = "TestSource.cs")
    {
        var compilation = CreateCompilation(source, assemblyName, path);
        var analyzer = new ProductionApiHttpAnalyzer();
        var analyzerOptions = new AnalyzerOptions([]);
        var compilationOptions = new CompilationWithAnalyzersOptions(
            analyzerOptions,
            onAnalyzerException: null,
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false);
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer], compilationOptions);

        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }
}
