using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace SharedKernel.Mediator.Analyzers;

/// <summary>
/// Reports declaration and usage diagnostics for implemented mediator features.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SharedKernelMediatorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        MediatorAnalyzerDescriptors.InvalidHandlerSignature,
        MediatorAnalyzerDescriptors.MissingCancellationToken,
        MediatorAnalyzerDescriptors.HandlerReturnTypeMismatch,
        MediatorAnalyzerDescriptors.MissingCancellationForwarding,
        MediatorAnalyzerDescriptors.InvalidPipelineGenericArity,
        MediatorAnalyzerDescriptors.HandlerShouldNotCallSender);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(InitializeCompilation);
    }

    private static void InitializeCompilation(CompilationStartAnalysisContext context)
    {
        var symbols = DiscoverySymbols.Create(context.Compilation);
        if (!symbols.IsComplete)
        {
            return;
        }

        var options = MediatorAnalyzerConfigOptions.Parse(context.Options.AnalyzerConfigOptionsProvider);
        var pipelineTypeParameterDiagnostics = new ConcurrentDictionary<string, Diagnostic>(StringComparer.Ordinal);

        context.RegisterSymbolAction(
            symbolContext => AnalyzeNamedType(symbolContext, symbols, pipelineTypeParameterDiagnostics),
            SymbolKind.NamedType);

        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation(operationContext, symbols, options),
            OperationKind.Invocation);
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        DiscoverySymbols symbols,
        ConcurrentDictionary<string, Diagnostic> pipelineTypeParameterDiagnostics)
    {
        if (context.Symbol is not INamedTypeSymbol type
            || type.TypeKind is TypeKind.Interface or TypeKind.Delegate
            || type.IsAbstract)
        {
            return;
        }

        AnalyzeHandlerType(context, type, symbols);
        AnalyzePipelineType(context, type, symbols, pipelineTypeParameterDiagnostics);
    }

    private static void AnalyzeHandlerType(SymbolAnalysisContext context, INamedTypeSymbol type, DiscoverySymbols symbols)
    {
        foreach (var handlerTypeArguments in type.AllInterfaces
                     .Where(candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.HandlerInterface))
                     .Select(static candidate => candidate.TypeArguments))
        {
            var requestType = handlerTypeArguments[0];
            var responseType = handlerTypeArguments[1];
            var compatibleHandle = FindPublicHandleMethod(type, requestType, responseType, symbols.CancellationTokenType, symbols.ValueTaskOfT);

            if (compatibleHandle is not null)
            {
                continue;
            }

            var methodsForRequest = type.GetMembers("Handle")
                .OfType<IMethodSymbol>()
                .Where(static method => !method.IsStatic && method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public)
                .Where(method => method.Parameters.Length > 0 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, requestType))
                .ToArray();

            var missingTokenMethod = methodsForRequest.FirstOrDefault(static method => method.Parameters.Length == 1);
            if (missingTokenMethod is not null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MediatorAnalyzerDescriptors.MissingCancellationToken,
                        GetDiagnosticLocation(missingTokenMethod, type),
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                continue;
            }

            var wrongReturnMethod = methodsForRequest.FirstOrDefault(
                method => method.Parameters.Length == 2
                          && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, symbols.CancellationTokenType)
                          && !SymbolEqualityComparer.Default.Equals(
                              method.ReturnType,
                              symbols.ValueTaskOfT.Construct(responseType)));

            if (wrongReturnMethod is not null)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MediatorAnalyzerDescriptors.HandlerReturnTypeMismatch,
                        GetDiagnosticLocation(wrongReturnMethod, type),
                        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        wrongReturnMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        symbols.ValueTaskOfT.Construct(responseType).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    MediatorAnalyzerDescriptors.InvalidHandlerSignature,
                    GetDiagnosticLocation(type),
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }
    }

    private static void AnalyzePipelineType(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        DiscoverySymbols symbols,
        ConcurrentDictionary<string, Diagnostic> pipelineTypeParameterDiagnostics)
    {
        if (!type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.PipelineInterface)))
        {
            return;
        }

        if (type.TypeParameters.Length is > 0 and not 2)
        {
            var diagnostic = Diagnostic.Create(
                MediatorAnalyzerDescriptors.InvalidPipelineGenericArity,
                GetDiagnosticLocation(type),
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                type.TypeParameters.Length);

            if (pipelineTypeParameterDiagnostics.TryAdd(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), diagnostic))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        DiscoverySymbols symbols,
        MediatorAnalyzerConfigOptions options)
    {
        if (context.Operation is not IInvocationOperation invocation
            || context.ContainingSymbol is not IMethodSymbol containingMethod
            || containingMethod.Parameters.All(parameter => !SymbolEqualityComparer.Default.Equals(parameter.Type, symbols.CancellationTokenType)))
        {
            return;
        }

        var availableCancellationToken = containingMethod.Parameters.First(
            parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, symbols.CancellationTokenType));

        if (options.EnableCancellationAnalysis && IsMediatorDispatchCall(invocation.TargetMethod, symbols))
        {
            var cancellationArgument = invocation.Arguments.LastOrDefault(
                argument => SymbolEqualityComparer.Default.Equals(argument.Parameter?.Type, symbols.CancellationTokenType));

            if (cancellationArgument is not null
                && cancellationArgument.Parameter is not null
                && !IsForwardedCancellationToken(cancellationArgument.Value, availableCancellationToken))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        MediatorAnalyzerDescriptors.MissingCancellationForwarding,
                        cancellationArgument.Syntax.GetLocation(),
                        invocation.TargetMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        availableCancellationToken.Name));
            }
        }

        if (options.CqrsStrict
            && !options.AllowHandlerToHandlerCalls
            && IsMediatorSendCall(invocation.TargetMethod, symbols)
            && IsHandlerType(containingMethod.ContainingType, symbols))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    MediatorAnalyzerDescriptors.HandlerShouldNotCallSender,
                    invocation.Syntax.GetLocation(),
                    containingMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }
    }

    private static IMethodSymbol? FindPublicHandleMethod(
        INamedTypeSymbol type,
        ITypeSymbol requestType,
        ITypeSymbol responseType,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol valueTaskOfT)
    {
        return type.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(
                method =>
                    !method.IsStatic
                    && method.MethodKind == MethodKind.Ordinary
                    && method.DeclaredAccessibility == Accessibility.Public
                    && method.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, requestType)
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, cancellationTokenType)
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTaskOfT.Construct(responseType)));
    }

    private static bool IsMediatorDispatchCall(IMethodSymbol method, DiscoverySymbols symbols)
    {
        return IsMediatorSendCall(method, symbols)
               || string.Equals(method.Name, "Publish", StringComparison.Ordinal)
               && ImplementsOrEquals(method.ContainingType, symbols.PublisherInterface);
    }

    private static bool IsMediatorSendCall(IMethodSymbol method, DiscoverySymbols symbols)
    {
        return string.Equals(method.Name, "Send", StringComparison.Ordinal)
               && ImplementsOrEquals(method.ContainingType, symbols.SenderInterface);
    }

    private static bool IsForwardedCancellationToken(IOperation operation, IParameterSymbol availableCancellationToken)
    {
        return operation is IParameterReferenceOperation parameterReference
               && SymbolEqualityComparer.Default.Equals(parameterReference.Parameter, availableCancellationToken);
    }

    private static bool IsHandlerType(INamedTypeSymbol? type, DiscoverySymbols symbols)
    {
        return type is not null
               && ImplementsOrEquals(type, symbols.HandlerInterface);
    }

    private static bool ImplementsOrEquals(INamedTypeSymbol type, INamedTypeSymbol contract)
    {
        return SymbolEqualityComparer.Default.Equals(type, contract)
               || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, contract)
               || type.AllInterfaces.Any(
                   candidate => SymbolEqualityComparer.Default.Equals(candidate, contract)
                                || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, contract));
    }

    private static Location GetDiagnosticLocation(ISymbol symbol)
    {
        return symbol.Locations.FirstOrDefault(static candidate => candidate.IsInSource)
               ?? symbol.Locations.FirstOrDefault()
               ?? Location.None;
    }

    private static Location GetDiagnosticLocation(IMethodSymbol method, INamedTypeSymbol fallbackType)
    {
        return GetDiagnosticLocation((ISymbol)method).SourceTree is not null
            ? GetDiagnosticLocation((ISymbol)method)
            : GetDiagnosticLocation(fallbackType);
    }

    private sealed class DiscoverySymbols(
        INamedTypeSymbol handlerInterface,
        INamedTypeSymbol pipelineInterface,
        INamedTypeSymbol senderInterface,
        INamedTypeSymbol publisherInterface,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol valueTaskOfT)
    {
        public INamedTypeSymbol HandlerInterface { get; } = handlerInterface;

        public INamedTypeSymbol PipelineInterface { get; } = pipelineInterface;

        public INamedTypeSymbol SenderInterface { get; } = senderInterface;

        public INamedTypeSymbol PublisherInterface { get; } = publisherInterface;

        public INamedTypeSymbol CancellationTokenType { get; } = cancellationTokenType;

        public INamedTypeSymbol ValueTaskOfT { get; } = valueTaskOfT;

        public bool IsComplete =>
            HandlerInterface is not null
            && PipelineInterface is not null
            && SenderInterface is not null
            && PublisherInterface is not null
            && CancellationTokenType is not null
            && ValueTaskOfT is not null;

        public static DiscoverySymbols Create(Compilation compilation)
        {
            return new DiscoverySymbols(
                compilation.GetTypeByMetadataName("SharedKernel.Mediator.IRequestHandler`2")!,
                compilation.GetTypeByMetadataName("SharedKernel.Mediator.IPipelineBehavior`2")!,
                compilation.GetTypeByMetadataName("SharedKernel.Mediator.ISender")!,
                compilation.GetTypeByMetadataName("SharedKernel.Mediator.IPublisher")!,
                compilation.GetTypeByMetadataName("System.Threading.CancellationToken")!,
                compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")!);
        }
    }
}
