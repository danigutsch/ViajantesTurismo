using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using SharedKernel.Mediator.SourceGenerator;

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
        MediatorAnalyzerDescriptors.MissingEnumeratorCancellation,
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
            symbolContext => AnalyzeNamedType(symbolContext, symbols, options, pipelineTypeParameterDiagnostics),
            SymbolKind.NamedType);

        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation(operationContext, symbols, options),
            OperationKind.Invocation);
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        DiscoverySymbols symbols,
        MediatorAnalyzerConfigOptions options,
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
        AnalyzeStreamType(context, type, symbols, options);
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
        if (!type.AllInterfaces.Any(
                candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.PipelineInterface)
                             || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.StreamPipelineInterface)))
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

    private static void AnalyzeStreamType(
        SymbolAnalysisContext context,
        INamedTypeSymbol type,
        DiscoverySymbols symbols,
        MediatorAnalyzerConfigOptions options)
    {
        if (!options.EnableCancellationAnalysis)
        {
            return;
        }

        foreach (var streamHandlerTypeArguments in type.AllInterfaces
                     .Where(candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.StreamHandlerInterface))
                     .Select(static candidate => candidate.TypeArguments))
        {
            var requestType = streamHandlerTypeArguments[0];
            var responseType = streamHandlerTypeArguments[1];
            var compatibleHandle = FindPublicStreamHandleMethod(
                type,
                requestType,
                responseType,
                symbols.CancellationTokenType,
                symbols.AsyncEnumerableOfT);

            ReportMissingEnumeratorCancellation(context, type, compatibleHandle, symbols);
        }

        foreach (var streamPipelineTypeArguments in type.AllInterfaces
                     .Where(candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, symbols.StreamPipelineInterface))
                     .Select(static candidate => candidate.TypeArguments))
        {
            var requestType = streamPipelineTypeArguments[0];
            var responseType = streamPipelineTypeArguments[1];
            var compatibleHandle = FindPublicStreamPipelineHandleMethod(
                type,
                requestType,
                responseType,
                symbols.StreamHandlerContinuation,
                symbols.CancellationTokenType,
                symbols.AsyncEnumerableOfT);

            ReportMissingEnumeratorCancellation(context, type, compatibleHandle, symbols);
        }
    }

    private static void ReportMissingEnumeratorCancellation(
        SymbolAnalysisContext context,
        INamedTypeSymbol containingType,
        IMethodSymbol? method,
        DiscoverySymbols symbols)
    {
        if (method is null
            || !method.IsAsync
            || method.Parameters.Length == 0)
        {
            return;
        }

        var cancellationTokenParameter = method.Parameters[method.Parameters.Length - 1];
        if (!SymbolEqualityComparer.Default.Equals(cancellationTokenParameter.Type, symbols.CancellationTokenType)
            || HasEnumeratorCancellationAttribute(cancellationTokenParameter, symbols.EnumeratorCancellationAttribute))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                MediatorAnalyzerDescriptors.MissingEnumeratorCancellation,
                GetDiagnosticLocation(cancellationTokenParameter, method, containingType),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                cancellationTokenParameter.Name));
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

    private static IMethodSymbol? FindPublicStreamHandleMethod(
        INamedTypeSymbol type,
        ITypeSymbol requestType,
        ITypeSymbol responseType,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol asyncEnumerableOfT)
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
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, asyncEnumerableOfT.Construct(responseType)));
    }

    private static IMethodSymbol? FindPublicStreamPipelineHandleMethod(
        INamedTypeSymbol type,
        ITypeSymbol requestType,
        ITypeSymbol responseType,
        INamedTypeSymbol streamHandlerContinuation,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol asyncEnumerableOfT)
    {
        return type.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(
                method =>
                    !method.IsStatic
                    && method.MethodKind == MethodKind.Ordinary
                    && method.DeclaredAccessibility == Accessibility.Public
                    && method.Parameters.Length == 3
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, requestType)
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, streamHandlerContinuation.Construct(responseType))
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[2].Type, cancellationTokenType)
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, asyncEnumerableOfT.Construct(responseType)));
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

    private static bool HasEnumeratorCancellationAttribute(
        IParameterSymbol parameter,
        INamedTypeSymbol enumeratorCancellationAttribute)
    {
        return parameter.GetAttributes().Any(
            attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, enumeratorCancellationAttribute));
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

    private static Location GetDiagnosticLocation(
        IParameterSymbol parameter,
        IMethodSymbol fallbackMethod,
        INamedTypeSymbol fallbackType)
    {
        return GetDiagnosticLocation((ISymbol)parameter).SourceTree is not null
            ? GetDiagnosticLocation((ISymbol)parameter)
            : GetDiagnosticLocation(fallbackMethod, fallbackType);
    }

    private sealed class DiscoverySymbols(
        INamedTypeSymbol handlerInterface,
        INamedTypeSymbol pipelineInterface,
        INamedTypeSymbol streamHandlerInterface,
        INamedTypeSymbol streamPipelineInterface,
        INamedTypeSymbol senderInterface,
        INamedTypeSymbol publisherInterface,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol valueTaskOfT,
        INamedTypeSymbol asyncEnumerableOfT,
        INamedTypeSymbol streamHandlerContinuation,
        INamedTypeSymbol enumeratorCancellationAttribute)
    {
        public INamedTypeSymbol HandlerInterface { get; } = handlerInterface;

        public INamedTypeSymbol PipelineInterface { get; } = pipelineInterface;

        public INamedTypeSymbol StreamHandlerInterface { get; } = streamHandlerInterface;

        public INamedTypeSymbol StreamPipelineInterface { get; } = streamPipelineInterface;

        public INamedTypeSymbol SenderInterface { get; } = senderInterface;

        public INamedTypeSymbol PublisherInterface { get; } = publisherInterface;

        public INamedTypeSymbol CancellationTokenType { get; } = cancellationTokenType;

        public INamedTypeSymbol ValueTaskOfT { get; } = valueTaskOfT;

        public INamedTypeSymbol AsyncEnumerableOfT { get; } = asyncEnumerableOfT;

        public INamedTypeSymbol StreamHandlerContinuation { get; } = streamHandlerContinuation;

        public INamedTypeSymbol EnumeratorCancellationAttribute { get; } = enumeratorCancellationAttribute;

        public bool IsComplete =>
            HandlerInterface is not null
            && PipelineInterface is not null
            && StreamHandlerInterface is not null
            && StreamPipelineInterface is not null
            && SenderInterface is not null
            && PublisherInterface is not null
            && CancellationTokenType is not null
            && ValueTaskOfT is not null
            && AsyncEnumerableOfT is not null
            && StreamHandlerContinuation is not null
            && EnumeratorCancellationAttribute is not null;

        public static DiscoverySymbols Create(Compilation compilation)
        {
            return new DiscoverySymbols(
                compilation.GetTypeByMetadataName(MetadataNames.RequestHandler)!,
                compilation.GetTypeByMetadataName(MetadataNames.PipelineBehavior)!,
                compilation.GetTypeByMetadataName(MetadataNames.StreamRequestHandler)!,
                compilation.GetTypeByMetadataName(MetadataNames.StreamPipelineBehavior)!,
                compilation.GetTypeByMetadataName(MetadataNames.Sender)!,
                compilation.GetTypeByMetadataName(MetadataNames.Publisher)!,
                compilation.GetTypeByMetadataName(MetadataNames.CancellationToken)!,
                compilation.GetTypeByMetadataName(MetadataNames.ValueTaskOfResponse)!,
                compilation.GetTypeByMetadataName(MetadataNames.AsyncEnumerableOfResponse)!,
                compilation.GetTypeByMetadataName(MetadataNames.StreamHandlerContinuation)!,
                compilation.GetTypeByMetadataName(MetadataNames.EnumeratorCancellationAttribute)!);
        }
    }
}
