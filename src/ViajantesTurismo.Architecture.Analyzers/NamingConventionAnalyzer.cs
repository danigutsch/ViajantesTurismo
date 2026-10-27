using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ViajantesTurismo.Architecture.Analyzers;

/// <summary>
/// Reports integration events and domain error classes whose names do not follow the repository suffix conventions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NamingConventionAnalyzer : DiagnosticAnalyzer
{
    private const string DomainAssemblySuffix = ".Domain";
    private const string ErrorClassSuffix = "Errors";
    private const string IntegrationEventMetadataName = "PandaBear.Messaging.IntegrationEvents.IIntegrationEvent";
    private const string IntegrationEventSuffix = "IntegrationEvent";
    private const string ResultMetadataName = "PandaBear.Results.Result";
    private const string GenericResultMetadataName = "PandaBear.Results.Result`1";

    private static readonly DiagnosticDescriptor IntegrationEventNameRule = new(
        ArchitectureDiagnosticIds.IntegrationEventName,
        title: "Integration events should end with IntegrationEvent",
        messageFormat: "Integration event '{0}' should have a name ending with '{1}'",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types implementing IIntegrationEvent are the cross-boundary contract and carry the IntegrationEvent suffix.");

    private static readonly DiagnosticDescriptor ErrorClassNameRule = new(
        ArchitectureDiagnosticIds.ErrorClassName,
        title: "Domain error classes should end with Errors",
        messageFormat: "Static class '{0}' only creates results and should be named with the '{1}' suffix",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Domain static classes whose public methods all return Result or Result<T> are error classes and carry the Errors suffix.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get;
    } =
        [IntegrationEventNameRule, ErrorClassNameRule];

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
            var integrationEvent = compilationContext.Compilation.GetTypeByMetadataName(IntegrationEventMetadataName);
            var result = compilationContext.Compilation.GetTypeByMetadataName(ResultMetadataName);
            var genericResult = compilationContext.Compilation.GetTypeByMetadataName(GenericResultMetadataName);
            var isDomainAssembly = compilationContext.Compilation.AssemblyName?.EndsWith(DomainAssemblySuffix, StringComparison.Ordinal) == true;

            compilationContext.RegisterSymbolAction(
                symbolContext =>
                {
                    var type = (INamedTypeSymbol)symbolContext.Symbol;
                    AnalyzeIntegrationEvent(symbolContext, type, integrationEvent);
                    if (isDomainAssembly)
                    {
                        AnalyzeErrorClass(symbolContext, type, result, genericResult);
                    }
                },
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeIntegrationEvent(SymbolAnalysisContext context, INamedTypeSymbol type, INamedTypeSymbol? integrationEvent)
    {
        if (integrationEvent is null
            || type.IsAbstract
            || type.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || type.Name.EndsWith(IntegrationEventSuffix, StringComparison.Ordinal)
            || !type.AllInterfaces.Contains(integrationEvent, SymbolEqualityComparer.Default))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(IntegrationEventNameRule, type.Locations[0], type.Name, IntegrationEventSuffix));
    }

    private static void AnalyzeErrorClass(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        INamedTypeSymbol? result,
        INamedTypeSymbol? genericResult)
    {
        if (result is null
            || genericResult is null
            || !type.IsStatic
            || type.TypeKind != TypeKind.Class
            || type.ContainingType is not null
            || type.Name.EndsWith(ErrorClassSuffix, StringComparison.Ordinal))
        {
            return;
        }

        var publicMethods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static method => method is { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public })
            .ToArray();

        if (publicMethods.Length == 0
            || !publicMethods.All(method => IsResult(method.ReturnType, result, genericResult)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(ErrorClassNameRule, type.Locations[0], type.Name, ErrorClassSuffix));
    }

    private static bool IsResult(ITypeSymbol returnType, INamedTypeSymbol result, INamedTypeSymbol genericResult)
    {
        return SymbolEqualityComparer.Default.Equals(returnType, result)
            || (returnType is INamedTypeSymbol named
                && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, genericResult));
    }
}
