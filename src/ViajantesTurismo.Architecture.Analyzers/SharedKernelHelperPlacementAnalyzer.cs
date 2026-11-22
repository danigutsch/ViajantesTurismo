using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViajantesTurismo.Architecture.Analyzers;

/// <summary>Reports SharedKernel placement candidates after checking their resolved dependencies.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharedKernelHelperPlacementAnalyzer : DiagnosticAnalyzer
{
    private const string CandidatesKey = "build_property.ViajantesTurismoSharedKernelHelperCandidates";
    private const string OwnersKey = "build_property.ViajantesTurismoSharedKernelCapabilityOwners";
    private static readonly HashSet<string> FrameworkFacadeAssemblies = new(StringComparer.Ordinal)
    {
        "mscorlib",
        "netstandard",
        "System",
    };

    private static readonly DiagnosticDescriptor Rule = new(
        ArchitectureDiagnosticIds.SharedKernelHelperPlacement,
        title: "Review a helper for SharedKernel placement",
        messageFormat: "Review {0} helper '{1}' for '{2}' in {3} using the resolved type dependencies {4}",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An opted-in helper whose dependencies fit a reusable boundary can be reviewed for extraction.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static compilation =>
        {
            var options = compilation.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            var candidates = ReadMappings(options, CandidatesKey);
            if (candidates.Count == 0)
            {
                return;
            }

            var owners = ReadMappings(options, OwnersKey);
            var assemblyName = compilation.Compilation.AssemblyName ?? string.Empty;
            var isTest = assemblyName.EndsWith("Tests", StringComparison.Ordinal)
                || assemblyName.EndsWith(".Testing", StringComparison.Ordinal);
            var evidence = new ConcurrentBag<(string Name, Location Location, ImmutableArray<string> Dependencies)>();
            compilation.RegisterSemanticModelAction(model => Collect(model, candidates, isTest, evidence));
            compilation.RegisterCompilationEndAction(end =>
            {
                foreach (var candidate in evidence.GroupBy(item => item.Name, StringComparer.Ordinal))
                {
                    if (candidate.Any(item => item.Dependencies.IsDefault))
                    {
                        continue;
                    }

                    var role = candidates[candidate.Key];
                    var destination = owners.TryGetValue(role, out var package)
                        && package.StartsWith("PandaBear.SharedKernel.", StringComparison.Ordinal)
                        ? package
                        : "a focused new SharedKernel library";
                    var types = candidate.SelectMany(item => item.Dependencies)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(type => type, StringComparer.Ordinal);
                    var location = candidate.Select(item => item.Location)
                        .OrderBy(item => item.GetLineSpan().Path, StringComparer.Ordinal)
                        .ThenBy(item => item.SourceSpan.Start)
                        .First();
                    end.ReportDiagnostic(Diagnostic.Create(
                        Rule, location, isTest ? "test" : "source", candidate.Key, role, destination,
                        string.Join(", ", types.DefaultIfEmpty("none"))));
                }
            });
        });
    }

    private static void Collect(
        SemanticModelAnalysisContext context,
        Dictionary<string, string> candidates,
        bool isTest,
        ConcurrentBag<(string Name, Location Location, ImmutableArray<string> Dependencies)> evidence)
    {
        var model = context.SemanticModel;
        var root = model.SyntaxTree.GetRoot(context.CancellationToken);
        foreach (var declaration in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration, context.CancellationToken) is not { TypeKind: TypeKind.Class, ContainingType: null } helper)
            {
                continue;
            }

            var name = helper.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (candidates.ContainsKey(name))
            {
                evidence.Add((name, declaration.Identifier.GetLocation(), ResolveDependencies(context, declaration, helper, isTest)));
            }
        }
    }

    private static ImmutableArray<string> ResolveDependencies(
        SemanticModelAnalysisContext context,
        TypeDeclarationSyntax declaration,
        INamedTypeSymbol helper,
        bool isTest)
    {
        var dependencies = new HashSet<string>(StringComparer.Ordinal);
        foreach (var expression in declaration.DescendantNodes().OfType<ExpressionSyntax>())
        {
            var info = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
            var symbol = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol;
            if (!AddType(info.Type, helper, isTest, dependencies)
                || !AddType(info.ConvertedType, helper, isTest, dependencies)
                || !AddType(symbol as ITypeSymbol ?? symbol?.ContainingType, helper, isTest, dependencies))
            {
                return default;
            }
        }

        return [.. dependencies];
    }

    private static bool AddType(ITypeSymbol? type, INamedTypeSymbol helper, bool isTest, HashSet<string> dependencies)
    {
        if (type is null or ITypeParameterSymbol)
        {
            return true;
        }

        if (type is IArrayTypeSymbol array)
        {
            return AddType(array.ElementType, helper, isTest, dependencies);
        }

        if (type is not INamedTypeSymbol named || type.TypeKind == TypeKind.Error
            || !named.TypeArguments.All(argument => AddType(argument, helper, isTest, dependencies)))
        {
            return false;
        }

        for (var containing = named; containing is not null; containing = containing.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(containing.OriginalDefinition, helper))
            {
                return true;
            }
        }

        var assembly = named.ContainingAssembly.Name;
        if (FrameworkFacadeAssemblies.Contains(assembly)
            || assembly.StartsWith("System.", StringComparison.Ordinal)
            || assembly.StartsWith("SharedKernel.", StringComparison.Ordinal)
            || (isTest && assembly.StartsWith("xunit.", StringComparison.OrdinalIgnoreCase)))
        {
            dependencies.Add(named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            return true;
        }

        return false;
    }

    private static Dictionary<string, string> ReadMappings(AnalyzerConfigOptions options, string key)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!options.TryGetValue(key, out var mappings))
        {
            return result;
        }

        foreach (var mapping in mappings.Split(['|'], StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = mapping.IndexOf('=');
            if (separator <= 0 || separator == mapping.Length - 1)
            {
                continue;
            }

            result[mapping.Substring(0, separator).Trim()] = mapping.Substring(separator + 1).Trim();
        }

        return result;
    }
}
