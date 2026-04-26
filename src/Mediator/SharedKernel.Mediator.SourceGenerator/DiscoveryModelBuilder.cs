using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Builds the aggregate discovery model for the initial report emitter.
/// </summary>
internal static class DiscoveryModelBuilder
{
    public static DiscoveryCounts Build(Compilation compilation, CancellationToken cancellationToken)
    {
        var discoverySymbols = DiscoverySymbols.Create(compilation);
        var moduleAttribute = compilation.GetTypeByMetadataName(MetadataNames.MediatorModuleAttribute);

        if (!discoverySymbols.IsComplete)
        {
            return new DiscoveryCounts(0, 0, 0);
        }

        var discoverySets = new DiscoverySets();

        foreach (var assembly in EnumerateAssemblies(compilation, moduleAttribute))
        {
            CollectTypes(assembly.GlobalNamespace, discoverySymbols, discoverySets, cancellationToken);
        }

        return new DiscoveryCounts(
            discoverySets.Requests.Count,
            discoverySets.Handlers.Count,
            discoverySets.Pipelines.Count);
    }

    private static IEnumerable<IAssemblySymbol> EnumerateAssemblies(
        Compilation compilation,
        INamedTypeSymbol? moduleAttribute)
    {
        yield return compilation.Assembly;

        if (moduleAttribute is null)
        {
            yield break;
        }

        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols.Where(
                     referencedAssembly => referencedAssembly.GetAttributes().Any(
                         attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, moduleAttribute))))
        {
            yield return referencedAssembly;
        }
    }

    private static void CollectTypes(
        INamespaceSymbol @namespace,
        DiscoverySymbols discoverySymbols,
        DiscoverySets discoverySets,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            CollectTypes(nestedNamespace, discoverySymbols, discoverySets, cancellationToken);
        }

        foreach (var type in @namespace.GetTypeMembers())
        {
            CollectType(type, discoverySymbols, discoverySets, cancellationToken);
        }
    }

    private static void CollectType(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoverySets discoverySets,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ImplementsGenericInterface(type, discoverySymbols.RequestInterface))
        {
            discoverySets.Requests.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (ImplementsGenericInterface(type, discoverySymbols.HandlerInterface))
        {
            discoverySets.Handlers.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (ImplementsGenericInterface(type, discoverySymbols.PipelineInterface))
        {
            discoverySets.Pipelines.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            CollectType(nestedType, discoverySymbols, discoverySets, cancellationToken);
        }
    }

    private static bool ImplementsGenericInterface(INamedTypeSymbol type, INamedTypeSymbol openGenericInterface)
    {
        return type.AllInterfaces.Any(
            candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, openGenericInterface));
    }

    private sealed class DiscoverySets
    {
        public HashSet<string> Requests { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Handlers { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Pipelines { get; } = new(StringComparer.Ordinal);
    }

    private sealed class DiscoverySymbols(
        INamedTypeSymbol? requestInterface,
        INamedTypeSymbol? handlerInterface,
        INamedTypeSymbol? pipelineInterface)
    {
        public bool IsComplete => RequestInterface is not null && HandlerInterface is not null && PipelineInterface is not null;

        public INamedTypeSymbol RequestInterface { get; } = requestInterface!;

        public INamedTypeSymbol HandlerInterface { get; } = handlerInterface!;

        public INamedTypeSymbol PipelineInterface { get; } = pipelineInterface!;

        public static DiscoverySymbols Create(Compilation compilation)
        {
            return new DiscoverySymbols(
                compilation.GetTypeByMetadataName(MetadataNames.Request),
                compilation.GetTypeByMetadataName(MetadataNames.RequestHandler),
                compilation.GetTypeByMetadataName(MetadataNames.PipelineBehavior));
        }
    }
}
