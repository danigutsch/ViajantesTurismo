using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViajantesTurismo.Architecture.Analyzers;

/// <summary>
/// Reports ordinary HTTP communication between repository-owned production API services.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProductionApiHttpAnalyzer : DiagnosticAnalyzer
{
    private const string AddHttpClientMethodName = "AddHttpClient";
    private const string AdminResourceValue = "api";
    private const string BaseAddressPropertyName = "BaseAddress";
    private const string BrandingApiClientInterfaceTypeName = "IBrandingApiClient";
    private const string BrandingApiClientTypeName = "BrandingApiClient";
    private const string CreateClientMethodName = "CreateClient";
    private const string CatalogResourceValue = "catalog-api";
    private const string HttpClientFactoryExtensionsTypeName = "HttpClientFactoryServiceCollectionExtensions";
    private const string HttpClientJsonExtensionsMetadataName = "System.Net.Http.Json.HttpClientJsonExtensions";
    private const string HttpClientTypeName = "HttpClient";
    private const string HttpClientFactoryTypeName = "IHttpClientFactory";
    private const string HttpRequestMessageTypeName = "HttpRequestMessage";
    private const string MicrosoftDependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";
    private const string RequestUriParameterName = "requestUri";
    private const string RequestUriPropertyName = "RequestUri";
    private const string RelativeUriParameterName = "relativeUri";
    private const string SharedKernelBrandingNamespace = "PandaBear.Branding";
    private const string SystemNetHttpNamespace = "System.Net.Http";
    private const string ResourceNamesTypeName = "ViajantesTurismo.Resources.ResourceNames";

    private static readonly DiagnosticDescriptor ProductionApiHttpRule = new(
        ArchitectureDiagnosticIds.ProductionApiHttp,
        title: "Production APIs should not call repository APIs over ordinary HTTP",
        messageFormat: "Production API project '{0}' must not call repository-owned destination '{1}' over ordinary HTTP; "
            + "use a versioned integration event, explicit gRPC, or an in-process module call",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Repository-owned production APIs communicate through integration events, explicitly approved gRPC, or in-process modules instead of ordinary API-to-API HTTP.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get;
    } =
        [ProductionApiHttpRule];

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
            var sourceApi = GetSourceApi(compilationContext.Compilation.AssemblyName);
            if (sourceApi == ApiDestination.None)
            {
                return;
            }

            var httpClientJsonExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(HttpClientJsonExtensionsMetadataName);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, sourceApi, httpClientJsonExtensionsType),
                OperationKind.Invocation);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeAssignment(operationContext, sourceApi),
                OperationKind.SimpleAssignment,
                OperationKind.CoalesceAssignment);
        });
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        ApiDestination sourceApi,
        INamedTypeSymbol? httpClientJsonExtensionsType)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        if (IsAddHttpClient(invocation.TargetMethod))
        {
            AnalyzeAddHttpClient(context, invocation, sourceApi);
            return;
        }

        if (IsHttpClientFactoryCreateClient(invocation.TargetMethod))
        {
            AnalyzeNamedClientInvocation(context, invocation, sourceApi);
            return;
        }

        if (IsHttpClientRequestInvocation(invocation, httpClientJsonExtensionsType))
        {
            AnalyzeHttpClientRequestInvocation(context, invocation, sourceApi);
        }
    }

    private static void AnalyzeAddHttpClient(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        ApiDestination sourceApi)
    {
        var destinations = new Dictionary<ApiDestination, Location>();

        foreach (var typeArgument in invocation.TargetMethod.TypeArguments)
        {
            var typeDestination = GetClientTypeDestination(typeArgument);
            if (typeDestination != ApiDestination.None)
            {
                destinations[typeDestination] = invocation.Syntax.GetLocation();
            }
        }

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.Type.SpecialType == SpecialType.System_String)
            {
                AddResolvedDestination(
                    argument.Value,
                    argument.Value.Syntax.GetLocation(),
                    destinations,
                    sourceApi,
                    DestinationResolutionMode.NamedClient);
            }

            AddBaseAddressDestinations(argument.Value, destinations, sourceApi);
        }

        foreach (var destination in destinations)
        {
            ReportIfCrossApi(context, destination.Key, sourceApi, destination.Value);
        }
    }

    private static void AnalyzeNamedClientInvocation(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        ApiDestination sourceApi)
    {
        var destinations = new Dictionary<ApiDestination, Location>();

        foreach (var argumentValue in invocation.Arguments.Select(static argument => argument.Value))
        {
            AddResolvedDestination(
                argumentValue,
                argumentValue.Syntax.GetLocation(),
                destinations,
                sourceApi,
                DestinationResolutionMode.NamedClient);
        }

        foreach (var destination in destinations)
        {
            ReportIfCrossApi(context, destination.Key, sourceApi, destination.Value);
        }
    }

    private static void AnalyzeHttpClientRequestInvocation(
        OperationAnalysisContext context,
        IInvocationOperation invocation,
        ApiDestination sourceApi)
    {
        var destinations = new Dictionary<ApiDestination, Location>();

        foreach (var argument in invocation.Arguments)
        {
            if (!IsRequestDestinationParameter(argument.Parameter))
            {
                continue;
            }

            AddResolvedDestination(
                argument.Value,
                argument.Value.Syntax.GetLocation(),
                destinations,
                sourceApi,
                DestinationResolutionMode.RequestUri);
        }

        foreach (var destination in destinations)
        {
            ReportIfCrossApi(context, destination.Key, sourceApi, destination.Value);
        }
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context, ApiDestination sourceApi)
    {
        if (context.Operation is not IAssignmentOperation assignment)
        {
            return;
        }

        var isBaseAddress = IsHttpClientBaseAddress(assignment.Target);
        var isRequestUri = IsHttpRequestUri(assignment.Target);
        if ((!isBaseAddress && !isRequestUri)
            || (isBaseAddress && IsInsideAddHttpClient(assignment))
            || (isRequestUri && IsInsideHttpRequestMessageCreation(assignment)))
        {
            return;
        }

        var destination = ResolveDestination(
            assignment.Value,
            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
            sourceApi,
            isBaseAddress ? DestinationResolutionMode.BaseAddress : DestinationResolutionMode.RequestUri);

        ReportIfCrossApi(context, destination, sourceApi, assignment.Value.Syntax.GetLocation());
    }

    private static void AddBaseAddressDestinations(
        IOperation operation,
        IDictionary<ApiDestination, Location> destinations,
        ApiDestination sourceApi)
    {
        if (operation is IAssignmentOperation assignment && IsHttpClientBaseAddress(assignment.Target))
        {
            AddResolvedDestination(
                assignment.Value,
                assignment.Value.Syntax.GetLocation(),
                destinations,
                sourceApi,
                DestinationResolutionMode.BaseAddress);
        }

        foreach (var child in operation.ChildOperations)
        {
            AddBaseAddressDestinations(child, destinations, sourceApi);
        }
    }

    private static void AddResolvedDestination(
        IOperation operation,
        Location location,
        IDictionary<ApiDestination, Location> destinations,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        var destination = ResolveDestination(
            operation,
            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
            sourceApi,
            resolutionMode);

        if (destination != ApiDestination.None)
        {
            destinations[destination] = location;
        }
    }

    private static ApiDestination ResolveDestination(
        IOperation operation,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        if (resolutionMode != DestinationResolutionMode.RequestUri
            && operation is IFieldReferenceOperation fieldReference)
        {
            var resourceNameDestination = GetResourceNameDestination(fieldReference.Field);
            if (resourceNameDestination != ApiDestination.None)
            {
                return resourceNameDestination;
            }
        }

        if (operation.ConstantValue is { HasValue: true, Value: string constantString })
        {
            var constantDestination = resolutionMode == DestinationResolutionMode.NamedClient
                ? GetNamedClientDestination(constantString)
                : GetKnownValueDestination(constantString, allowBareResourceName: false);
            if (constantDestination != ApiDestination.None)
            {
                return constantDestination;
            }
        }

        return operation switch
        {
            IConversionOperation conversion => ResolveDestination(conversion.Operand, visitedSymbols, sourceApi, resolutionMode),
            IParenthesizedOperation parenthesized => ResolveDestination(parenthesized.Operand, visitedSymbols, sourceApi, resolutionMode),
            IArgumentOperation argument => ResolveDestination(argument.Value, visitedSymbols, sourceApi, resolutionMode),
            ILocalReferenceOperation localReference => ResolveLocalDestination(localReference, visitedSymbols, sourceApi, resolutionMode),
            IObjectCreationOperation objectCreation when IsUriType(objectCreation.Type) => ResolveUriCreationDestination(
                objectCreation,
                visitedSymbols,
                sourceApi,
                resolutionMode),
            IObjectCreationOperation objectCreation when IsHttpRequestMessageType(objectCreation.Type) => ResolveHttpRequestMessageDestination(
                objectCreation,
                visitedSymbols,
                sourceApi,
                resolutionMode),
            IInterpolatedStringOperation interpolatedString when resolutionMode == DestinationResolutionMode.NamedClient => ResolveComposedNamedClientDestination(
                interpolatedString,
                visitedSymbols),
            IInterpolatedStringOperation interpolatedString => ResolveComposedUriDestination(
                interpolatedString,
                visitedSymbols,
                sourceApi),
            IInterpolationOperation interpolation => ResolveDestination(interpolation.Expression, visitedSymbols, sourceApi, resolutionMode),
            IBinaryOperation binary when resolutionMode == DestinationResolutionMode.NamedClient && binary.Type?.SpecialType == SpecialType.System_String => ResolveComposedNamedClientDestination(
                binary,
                visitedSymbols),
            IBinaryOperation binary when binary.Type?.SpecialType == SpecialType.System_String => ResolveComposedUriDestination(
                binary,
                visitedSymbols,
                sourceApi),
            IBinaryOperation binary => ResolveFirstDestination([binary.LeftOperand, binary.RightOperand], visitedSymbols, sourceApi, resolutionMode),
            IConditionalOperation conditional when conditional.Condition.ConstantValue is { HasValue: true, Value: bool condition } => ResolveFirstDestination(
                [condition ? conditional.WhenTrue : conditional.WhenFalse],
                visitedSymbols,
                sourceApi,
                resolutionMode),
            IConditionalOperation conditional => ResolveBranchDestination([conditional.WhenTrue, conditional.WhenFalse], visitedSymbols, sourceApi, resolutionMode),
            ICoalesceOperation coalesce when IsStaticallyNull(coalesce.Value, visitedSymbols) is bool isNull => ResolveDestination(
                isNull ? coalesce.WhenNull : coalesce.Value,
                visitedSymbols,
                sourceApi,
                resolutionMode),
            ICoalesceOperation coalesce => ResolveBranchDestination([coalesce.Value, coalesce.WhenNull], visitedSymbols, sourceApi, resolutionMode),
            _ => ApiDestination.None,
        };
    }

    private static ApiDestination ResolveFirstDestination(
        IEnumerable<IOperation?> operations,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        foreach (var operation in operations)
        {
            if (operation is null)
            {
                continue;
            }

            var destination = ResolveDestination(operation, visitedSymbols, sourceApi, resolutionMode);
            if (destination != ApiDestination.None)
            {
                return destination;
            }
        }

        return ApiDestination.None;
    }

    private static ApiDestination ResolveUriCreationDestination(
        IObjectCreationOperation objectCreation,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        var uriKindArgument = objectCreation.Arguments.FirstOrDefault(static argument =>
            string.Equals(argument.Parameter?.Name, "uriKind", StringComparison.Ordinal));
        if (uriKindArgument?.Value.ConstantValue is { HasValue: true, Value: int uriKindValue }
            && (UriKind)uriKindValue == UriKind.Relative)
        {
            return ApiDestination.None;
        }

        var relativeUriArgument = objectCreation.Arguments.FirstOrDefault(static argument =>
            string.Equals(argument.Parameter?.Name, RelativeUriParameterName, StringComparison.Ordinal));
        if (relativeUriArgument is not null)
        {
            var overrideDestination = ResolveDestination(
                relativeUriArgument.Value,
                visitedSymbols,
                sourceApi,
                DestinationResolutionMode.RequestUri);
            if (overrideDestination != ApiDestination.None)
            {
                return overrideDestination;
            }

            if (IsStaticallyBaseRelativeUri(relativeUriArgument.Value, visitedSymbols) is not true)
            {
                return ApiDestination.None;
            }

            var baseUriArgument = objectCreation.Arguments.FirstOrDefault(static argument =>
                IsUriType(argument.Parameter?.Type));
            return baseUriArgument is null
                ? ApiDestination.None
                : ResolveDestination(baseUriArgument.Value, visitedSymbols, sourceApi, resolutionMode);
        }

        foreach (var argument in objectCreation.Arguments)
        {
            var destination = ResolveDestination(argument.Value, visitedSymbols, sourceApi, resolutionMode);
            if (destination != ApiDestination.None)
            {
                return destination;
            }
        }

        return ApiDestination.None;
    }

    private static bool? IsStaticallyBaseRelativeUri(IOperation operation, ISet<ISymbol> visitedSymbols)
    {
        while (operation is IConversionOperation { IsTryCast: false, OperatorMethod: null } conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is IParenthesizedOperation parenthesized)
        {
            return IsStaticallyBaseRelativeUri(parenthesized.Operand, visitedSymbols);
        }

        if (operation.ConstantValue is { HasValue: true, Value: string value })
        {
            var candidate = value.TrimStart();
            return Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri)
                ? !uri.IsAbsoluteUri
                    && !candidate.StartsWith("//", StringComparison.Ordinal)
                    && !candidate.StartsWith("\\/", StringComparison.Ordinal)
                : null;
        }

        if (operation is ILocalReferenceOperation localReference)
        {
            if (!visitedSymbols.Add(localReference.Local))
            {
                return null;
            }

            try
            {
                return TryGetReachingLocalInitializer(localReference, out var initializer)
                    && initializer is not null
                        ? IsStaticallyBaseRelativeUri(initializer, visitedSymbols)
                        : null;
            }
            finally
            {
                visitedSymbols.Remove(localReference.Local);
            }
        }

        if (operation is not IObjectCreationOperation objectCreation || !IsUriType(objectCreation.Type))
        {
            return null;
        }

        var uriStringArgument = objectCreation.Arguments.FirstOrDefault(static argument =>
            string.Equals(argument.Parameter?.Name, "uriString", StringComparison.Ordinal));
        if (uriStringArgument is null)
        {
            return null;
        }

        var inferredKind = IsStaticallyBaseRelativeUri(uriStringArgument.Value, visitedSymbols);
        var uriKindArgument = objectCreation.Arguments.FirstOrDefault(static argument =>
            string.Equals(argument.Parameter?.Name, "uriKind", StringComparison.Ordinal));
        if (uriKindArgument is null)
        {
            return objectCreation.Arguments.Length == 1 ? inferredKind : null;
        }

        return uriKindArgument.Value.ConstantValue is { HasValue: true, Value: int uriKindValue }
            ? (UriKind)uriKindValue switch
            {
                UriKind.Relative when inferredKind is true => true,
                UriKind.Absolute when inferredKind is false => false,
                UriKind.RelativeOrAbsolute => inferredKind,
                _ => null
            }
            : null;
    }

    private static bool? IsStaticallyNull(IOperation operation, ISet<ISymbol> visitedSymbols)
    {
        while (operation is IConversionOperation { IsTryCast: false, OperatorMethod: null } conversion)
        {
            operation = conversion.Operand;
        }

        if (operation is IParenthesizedOperation parenthesized)
        {
            return IsStaticallyNull(parenthesized.Operand, visitedSymbols);
        }

        if (operation.ConstantValue is { HasValue: true } constantValue)
        {
            return constantValue.Value is null;
        }

        if (operation is IFieldReferenceOperation { Field.HasConstantValue: true } fieldReference)
        {
            return fieldReference.Field.ConstantValue is null;
        }

        if (operation is IObjectCreationOperation)
        {
            return false;
        }

        if (operation is not ILocalReferenceOperation localReference
            || !visitedSymbols.Add(localReference.Local))
        {
            return null;
        }

        try
        {
            return TryGetReachingLocalInitializer(localReference, out var initializer)
                && initializer is not null
                    ? IsStaticallyNull(initializer, visitedSymbols)
                    : null;
        }
        finally
        {
            visitedSymbols.Remove(localReference.Local);
        }
    }

    private static ApiDestination ResolveComposedNamedClientDestination(
        IOperation operation,
        ISet<ISymbol> visitedSymbols)
    {
        var candidate = new StringBuilder();
        return TryAppendNamedClientComponent(operation, candidate, visitedSymbols)
            ? GetNamedClientDestination(candidate.ToString())
            : ApiDestination.None;
    }

    private static bool TryAppendNamedClientComponent(
        IOperation operation,
        StringBuilder candidate,
        ISet<ISymbol> visitedSymbols)
    {
        if (operation.ConstantValue is { HasValue: true, Value: string constantValue })
        {
            candidate.Append(constantValue);
            return true;
        }

        switch (operation)
        {
            case IConversionOperation conversion:
                return TryAppendNamedClientComponent(conversion.Operand, candidate, visitedSymbols);
            case IParenthesizedOperation parenthesized:
                return TryAppendNamedClientComponent(parenthesized.Operand, candidate, visitedSymbols);
            case IBinaryOperation binary when binary.Type?.SpecialType == SpecialType.System_String:
                return TryAppendNamedClientComponent(binary.LeftOperand, candidate, visitedSymbols)
                    && TryAppendNamedClientComponent(binary.RightOperand, candidate, visitedSymbols);
            case IInterpolatedStringOperation interpolatedString:
                return interpolatedString.Parts.All(part => TryAppendNamedClientComponent(part, candidate, visitedSymbols));
            case IInterpolatedStringTextOperation textOperation
                when textOperation.Text.ConstantValue is { HasValue: true, Value: string text }:
                candidate.Append(text);
                return true;
            case IInterpolationOperation interpolation
                when interpolation.Alignment is null && interpolation.FormatString is null:
                return TryAppendNamedClientComponent(interpolation.Expression, candidate, visitedSymbols);
            case IInterpolationOperation:
                return false;
            case IFieldReferenceOperation fieldReference:
                var fieldDestination = GetResourceNameDestination(fieldReference.Field);
                if (fieldDestination == ApiDestination.None)
                {
                    return false;
                }

                candidate.Append(GetResourceValue(fieldDestination));
                return true;
            case ILocalReferenceOperation localReference:
                if (!visitedSymbols.Add(localReference.Local))
                {
                    return false;
                }

                try
                {
                    return TryGetReachingLocalInitializer(localReference, out var initializer)
                        && initializer is not null
                        && TryAppendNamedClientComponent(initializer, candidate, visitedSymbols);
                }
                finally
                {
                    visitedSymbols.Remove(localReference.Local);
                }
            default:
                return false;
        }
    }

    private static ApiDestination ResolveComposedUriDestination(
        IOperation operation,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi)
    {
        var candidate = new StringBuilder();
        var resolvedDestination = AppendUriComponent(
            operation,
            candidate,
            visitedSymbols,
            sourceApi);

        return resolvedDestination
            ?? GetKnownValueDestination(candidate.ToString(), allowBareResourceName: false);
    }

    private static ApiDestination? AppendUriComponent(
        IOperation operation,
        StringBuilder candidate,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi)
    {
        if (operation is IBinaryOperation binary)
        {
            var leftDestination = AppendUriComponent(binary.LeftOperand, candidate, visitedSymbols, sourceApi);
            return leftDestination
                ?? AppendUriComponent(binary.RightOperand, candidate, visitedSymbols, sourceApi);
        }

        if (operation is IInterpolatedStringOperation interpolatedString)
        {
            foreach (var part in interpolatedString.Parts)
            {
                var partDestination = AppendUriComponent(part, candidate, visitedSymbols, sourceApi);
                if (partDestination is not null)
                {
                    return partDestination;
                }
            }

            return null;
        }

        if (operation is ILocalReferenceOperation localReference)
        {
            if (!visitedSymbols.Add(localReference.Local))
            {
                return ApiDestination.None;
            }

            try
            {
                return TryGetReachingLocalInitializer(localReference, out var initializer)
                    && initializer is not null
                        ? AppendUriComponent(initializer, candidate, visitedSymbols, sourceApi)
                        : ApiDestination.None;
            }
            finally
            {
                visitedSymbols.Remove(localReference.Local);
            }
        }

        if (operation is IInterpolatedStringTextOperation textOperation
            && textOperation.Text.ConstantValue is { HasValue: true, Value: string text })
        {
            candidate.Append(text);
        }
        else if (operation is IInterpolationOperation interpolation)
        {
            if (interpolation.Alignment is not null || interpolation.FormatString is not null)
            {
                return ApiDestination.None;
            }

            return AppendUriComponent(interpolation.Expression, candidate, visitedSymbols, sourceApi);
        }
        else if (operation.ConstantValue is { HasValue: true, Value: string constantValue })
        {
            candidate.Append(constantValue);
        }
        else
        {
            var destination = ResolveDestination(
                operation,
                visitedSymbols,
                sourceApi,
                DestinationResolutionMode.NamedClient);
            if (destination == ApiDestination.None)
            {
                return ApiDestination.None;
            }

            candidate.Append(GetResourceValue(destination));
        }

        return TryResolveCompletedUriAuthority(candidate, out var completedDestination)
            ? completedDestination
            : null;
    }

    private static bool TryResolveCompletedUriAuthority(
        StringBuilder candidate,
        out ApiDestination destination)
    {
        var value = candidate.ToString();
        var schemeSeparator = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparator < 0)
        {
            destination = ApiDestination.None;
            return false;
        }

        var authorityStart = schemeSeparator + 3;
        var authorityEnd = value.IndexOfAny(['/', '?', '#'], authorityStart);
        if (authorityEnd < 0)
        {
            destination = ApiDestination.None;
            return false;
        }

        destination = GetKnownValueDestination(value.Substring(0, authorityEnd), allowBareResourceName: false);
        return true;
    }

    private static ApiDestination ResolveBranchDestination(
        IEnumerable<IOperation?> operations,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        var sourceDestination = ApiDestination.None;

        foreach (var operation in operations)
        {
            if (operation is null)
            {
                continue;
            }

            var destination = ResolveDestination(operation, visitedSymbols, sourceApi, resolutionMode);
            if (destination == ApiDestination.None)
            {
                continue;
            }

            if (destination != sourceApi)
            {
                return destination;
            }

            sourceDestination = destination;
        }

        return sourceDestination;
    }

    private static ApiDestination ResolveHttpRequestMessageDestination(
        IObjectCreationOperation objectCreation,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        if (objectCreation.Initializer is not null)
        {
            foreach (var operation in objectCreation.Initializer.ChildOperations)
            {
                if (operation is not IAssignmentOperation assignment || !IsHttpRequestUri(assignment.Target))
                {
                    continue;
                }

                return ResolveDestination(assignment.Value, visitedSymbols, sourceApi, resolutionMode);
            }
        }

        foreach (var argument in objectCreation.Arguments)
        {
            if (!IsRequestUriParameter(argument.Parameter))
            {
                continue;
            }

            var destination = ResolveDestination(argument.Value, visitedSymbols, sourceApi, resolutionMode);
            if (destination != ApiDestination.None)
            {
                return destination;
            }
        }

        return ApiDestination.None;
    }

    private static ApiDestination ResolveLocalDestination(
        ILocalReferenceOperation localReference,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        DestinationResolutionMode resolutionMode)
    {
        if (!visitedSymbols.Add(localReference.Local))
        {
            return ApiDestination.None;
        }

        try
        {
            return !TryGetReachingLocalInitializer(localReference, out var initializer)
                || initializer is null
                ? ApiDestination.None
                : ResolveDestination(initializer, visitedSymbols, sourceApi, resolutionMode);
        }
        finally
        {
            visitedSymbols.Remove(localReference.Local);
        }
    }

    private static bool TryGetReachingLocalInitializer(
        ILocalReferenceOperation localReference,
        out IOperation? initializer)
    {
        initializer = null;
        var root = (IOperation)localReference;
        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        var isAmbiguous = false;
        FindReachingLocalInitializer(
            root,
            localReference.Local,
            localReference,
            ref initializer,
            ref isAmbiguous);
        return !isAmbiguous && initializer is not null;
    }

    private static void FindReachingLocalInitializer(
        IOperation operation,
        ILocalSymbol local,
        ILocalReferenceOperation localReference,
        ref IOperation? initializer,
        ref bool isAmbiguous)
    {
        if (operation is IVariableDeclaratorOperation declarator
            && SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
        {
            initializer = declarator.Initializer?.Value;
            isAmbiguous = false;
        }
        else if (operation.Syntax.SpanStart < localReference.Syntax.SpanStart
            && IsWriteToLocal(operation, local, localReference))
        {
            if (operation is ISimpleAssignmentOperation assignment
                && assignment.Target is ILocalReferenceOperation assignedLocal
                && SymbolEqualityComparer.Default.Equals(assignedLocal.Local, local)
                && !assignment.Value.Syntax.Span.Contains(localReference.Syntax.Span)
                && assignment.Parent is IExpressionStatementOperation expressionStatement
                && expressionStatement.Parent is IBlockOperation assignmentBlock
                && IsOperationAncestor(assignmentBlock, localReference))
            {
                initializer = assignment.Value;
                isAmbiguous = false;
            }
            else
            {
                initializer = null;
                isAmbiguous = true;
            }
        }

        foreach (var child in operation.ChildOperations)
        {
            FindReachingLocalInitializer(child, local, localReference, ref initializer, ref isAmbiguous);
        }
    }

    private static bool IsOperationAncestor(IOperation candidate, IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWriteToLocal(
        IOperation operation,
        ILocalSymbol local,
        ILocalReferenceOperation localReference)
    {
        return IsWholeLocalWrite(operation, local)
            || operation is IAssignmentOperation assignment
                && IsHttpRequestUriMutationOfLocal(assignment.Target, localReference);
    }

    private static bool IsWholeLocalWrite(IOperation operation, ILocalSymbol local)
    {
        return operation switch
        {
            IAssignmentOperation assignment =>
                assignment.Target is ILocalReferenceOperation assignedLocal
                    && SymbolEqualityComparer.Default.Equals(assignedLocal.Local, local),
            IIncrementOrDecrementOperation incrementOrDecrement => ContainsLocalReference(incrementOrDecrement.Target, local),
            IArgumentOperation argument when argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out => ContainsLocalReference(argument.Value, local),
            _ => false
        };
    }

    private static bool IsHttpRequestUriMutationOfLocal(
        IOperation target,
        ILocalReferenceOperation localReference)
    {
        return target is IPropertyReferenceOperation { Instance: not null } propertyReference
            && IsHttpRequestUri(propertyReference)
            && ReferencesLocalOrDefiniteAlias(
                propertyReference.Instance,
                localReference,
                new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default));
    }

    private static bool ReferencesLocalOrDefiniteAlias(
        IOperation operation,
        ILocalReferenceOperation targetReference,
        ISet<ILocalSymbol> visitedAliases)
    {
        if (operation is IConversionOperation conversion)
        {
            return ReferencesLocalOrDefiniteAlias(conversion.Operand, targetReference, visitedAliases);
        }

        if (operation is IParenthesizedOperation parenthesized)
        {
            return ReferencesLocalOrDefiniteAlias(parenthesized.Operand, targetReference, visitedAliases);
        }

        if (operation is not ILocalReferenceOperation candidateReference)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(
            candidateReference.Local,
            targetReference.Local))
        {
            return IsSameLocalGeneration(candidateReference, targetReference);
        }

        if (!visitedAliases.Add(candidateReference.Local))
        {
            return false;
        }

        try
        {
            return TryGetReachingLocalInitializer(candidateReference, out var initializer)
                && initializer is not null
                && ReferencesLocalOrDefiniteAlias(initializer, targetReference, visitedAliases);
        }
        finally
        {
            visitedAliases.Remove(candidateReference.Local);
        }
    }

    private static bool IsSameLocalGeneration(
        ILocalReferenceOperation capturedReference,
        ILocalReferenceOperation currentReference)
    {
        var root = (IOperation)currentReference;
        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        return !ContainsWholeLocalWriteBetween(
            root,
            currentReference.Local,
            capturedReference.Syntax.SpanStart,
            currentReference.Syntax.SpanStart);
    }

    private static bool ContainsWholeLocalWriteBetween(
        IOperation operation,
        ILocalSymbol local,
        int startPosition,
        int endPosition)
    {
        if (operation.Syntax.SpanStart > startPosition
            && operation.Syntax.SpanStart < endPosition
            && IsWholeLocalWrite(operation, local))
        {
            return true;
        }

        return operation.ChildOperations.Any(child => ContainsWholeLocalWriteBetween(
            child,
            local,
            startPosition,
            endPosition));
    }

    private static bool ContainsLocalReference(IOperation operation, ILocalSymbol local)
    {
        if (operation is ILocalReferenceOperation localReference
            && SymbolEqualityComparer.Default.Equals(localReference.Local, local))
        {
            return true;
        }

        return operation.ChildOperations.Any(child => ContainsLocalReference(child, local));
    }

    private static ApiDestination GetKnownValueDestination(string value, bool allowBareResourceName)
    {
        var candidate = value.Trim();
        if (allowBareResourceName)
        {
            var directDestination = GetResourceValueDestination(candidate);
            if (directDestination != ApiDestination.None)
            {
                return directDestination;
            }
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            ? GetResourceValueDestination(uri.Host)
            : ApiDestination.None;
    }

    private static ApiDestination GetResourceValueDestination(string value)
    {
        if (string.Equals(value, AdminResourceValue, StringComparison.OrdinalIgnoreCase))
        {
            return ApiDestination.Admin;
        }

        if (string.Equals(value, CatalogResourceValue, StringComparison.OrdinalIgnoreCase))
        {
            return ApiDestination.Catalog;
        }

        return ApiDestination.None;
    }

    private static ApiDestination GetNamedClientDestination(string value)
    {
        if (string.Equals(value, AdminResourceValue, StringComparison.Ordinal))
        {
            return ApiDestination.Admin;
        }

        if (string.Equals(value, CatalogResourceValue, StringComparison.Ordinal))
        {
            return ApiDestination.Catalog;
        }

        return ApiDestination.None;
    }

    private static string GetResourceValue(ApiDestination destination)
    {
        return destination switch
        {
            ApiDestination.Admin => AdminResourceValue,
            ApiDestination.Catalog => CatalogResourceValue,
            _ => string.Empty
        };
    }

    private static ApiDestination GetResourceNameDestination(IFieldSymbol field)
    {
        if (!string.Equals(field.ContainingType.ToDisplayString(), ResourceNamesTypeName, StringComparison.Ordinal))
        {
            return ApiDestination.None;
        }

        return field.Name switch
        {
            "Api" => ApiDestination.Admin,
            "CatalogApi" => ApiDestination.Catalog,
            _ => ApiDestination.None
        };
    }

    private static ApiDestination GetClientTypeDestination(ITypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace.ToDisplayString();
        if (IsNamespaceOrChild(namespaceName, "ViajantesTurismo.Admin.Contracts.Http"))
        {
            return ApiDestination.Admin;
        }

        // Catalog.ApiService hosts the Branding routes, so Branding HTTP clients reach the Catalog API.
        if (IsNamespaceOrChild(namespaceName, "ViajantesTurismo.Catalog.Contracts.Http")
            || IsNamespaceOrChild(namespaceName, "ViajantesTurismo.Branding.Contracts.Http")
            || (string.Equals(namespaceName, SharedKernelBrandingNamespace, StringComparison.Ordinal)
                && type.Name is BrandingApiClientInterfaceTypeName or BrandingApiClientTypeName))
        {
            return ApiDestination.Catalog;
        }

        return ApiDestination.None;
    }

    private static ApiDestination GetSourceApi(string? assemblyName)
    {
        return assemblyName switch
        {
            "ViajantesTurismo.Admin.ApiService" => ApiDestination.Admin,
            "ViajantesTurismo.Catalog.ApiService" => ApiDestination.Catalog,
            _ => ApiDestination.None
        };
    }

    private static bool IsAddHttpClient(IMethodSymbol method)
    {
        var definition = method.ReducedFrom ?? method;
        return string.Equals(definition.Name, AddHttpClientMethodName, StringComparison.Ordinal)
            && string.Equals(definition.ContainingType.Name, HttpClientFactoryExtensionsTypeName, StringComparison.Ordinal)
            && string.Equals(definition.ContainingNamespace.ToDisplayString(), MicrosoftDependencyInjectionNamespace, StringComparison.Ordinal);
    }

    private static bool IsHttpClientFactoryCreateClient(IMethodSymbol method)
    {
        return string.Equals(method.Name, CreateClientMethodName, StringComparison.Ordinal)
            && string.Equals(method.ContainingType.Name, HttpClientFactoryTypeName, StringComparison.Ordinal)
            && string.Equals(method.ContainingNamespace.ToDisplayString(), SystemNetHttpNamespace, StringComparison.Ordinal)
            && method.Parameters.Length == 1
            && method.Parameters[0].Type.SpecialType == SpecialType.System_String
            && IsExactHttpClientType(method.ReturnType);
    }

    private static bool IsHttpClientRequestInvocation(
        IInvocationOperation invocation,
        INamedTypeSymbol? httpClientJsonExtensionsType)
    {
        if (IsExactHttpClientType(invocation.TargetMethod.ContainingType))
        {
            return true;
        }

        var definition = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        return definition.IsExtensionMethod
            && httpClientJsonExtensionsType is not null
            && SymbolEqualityComparer.Default.Equals(definition.ContainingType, httpClientJsonExtensionsType)
            && definition.Name is "GetFromJsonAsync"
                or "GetFromJsonAsAsyncEnumerable"
                or "PostAsJsonAsync"
                or "PutAsJsonAsync"
                or "PatchAsJsonAsync"
                or "DeleteFromJsonAsync"
            && definition.Parameters.Length > 0
            && IsHttpClientType(definition.Parameters[0].Type);
    }

    private static bool IsRequestDestinationParameter(IParameterSymbol? parameter)
    {
        return IsRequestUriParameter(parameter) || IsHttpRequestMessageType(parameter?.Type);
    }

    private static bool IsRequestUriParameter(IParameterSymbol? parameter)
    {
        return parameter is not null
            && string.Equals(parameter.Name, RequestUriParameterName, StringComparison.Ordinal)
            && (parameter.Type.SpecialType == SpecialType.System_String || IsUriType(parameter.Type));
    }

    private static bool IsHttpRequestUri(IOperation operation)
    {
        return operation is IPropertyReferenceOperation propertyReference
            && string.Equals(propertyReference.Property.Name, RequestUriPropertyName, StringComparison.Ordinal)
            && IsHttpRequestMessageType(propertyReference.Property.ContainingType);
    }

    private static bool IsHttpClientBaseAddress(IOperation operation)
    {
        return operation is IPropertyReferenceOperation propertyReference
            && string.Equals(propertyReference.Property.Name, BaseAddressPropertyName, StringComparison.Ordinal)
            && IsHttpClientType(propertyReference.Property.ContainingType);
    }

    private static bool IsInsideAddHttpClient(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IInvocationOperation invocation && IsAddHttpClient(invocation.TargetMethod))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideHttpRequestMessageCreation(IOperation operation)
    {
        for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IObjectCreationOperation objectCreation && IsHttpRequestMessageType(objectCreation.Type))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsHttpClientType(ITypeSymbol? type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (IsExactHttpClientType(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExactHttpClientType(ITypeSymbol? type)
    {
        return type is not null
            && string.Equals(type.Name, HttpClientTypeName, StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace.ToDisplayString(), SystemNetHttpNamespace, StringComparison.Ordinal);
    }

    private static bool IsHttpRequestMessageType(ITypeSymbol? type)
    {
        return type is not null
            && string.Equals(type.Name, HttpRequestMessageTypeName, StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace.ToDisplayString(), SystemNetHttpNamespace, StringComparison.Ordinal);
    }

    private static bool IsNamespaceOrChild(string namespaceName, string expectedNamespace)
    {
        return string.Equals(namespaceName, expectedNamespace, StringComparison.Ordinal)
            || namespaceName.StartsWith(expectedNamespace + ".", StringComparison.Ordinal);
    }

    private static bool IsUriType(ITypeSymbol? type)
    {
        return type is not null
            && string.Equals(type.Name, nameof(Uri), StringComparison.Ordinal)
            && string.Equals(type.ContainingNamespace.ToDisplayString(), "System", StringComparison.Ordinal);
    }

    private static void ReportIfCrossApi(
        OperationAnalysisContext context,
        ApiDestination destination,
        ApiDestination sourceApi,
        Location location)
    {
        if (destination == ApiDestination.None || destination == sourceApi)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                ProductionApiHttpRule,
                location,
                GetApiDisplayName(sourceApi),
                GetDestinationDisplayName(destination)));
    }

    private static string GetApiDisplayName(ApiDestination destination)
    {
        return destination switch
        {
            ApiDestination.Admin => "Admin API",
            ApiDestination.Catalog => "Catalog API",
            _ => "Unknown API"
        };
    }

    private static string GetDestinationDisplayName(ApiDestination destination)
    {
        return destination switch
        {
            ApiDestination.Admin => "Admin API ('api')",
            ApiDestination.Catalog => "Catalog API ('catalog-api')",
            _ => "unknown API"
        };
    }

    private enum DestinationResolutionMode
    {
        NamedClient,
        BaseAddress,
        RequestUri
    }

    private enum ApiDestination
    {
        None,
        Admin,
        Catalog
    }
}
