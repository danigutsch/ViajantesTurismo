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
    private const string BrandingResourceValue = "branding-api";
    private const string BrandingApiClientInterfaceTypeName = "IBrandingApiClient";
    private const string BrandingApiClientTypeName = "BrandingApiClient";
    private const string CreateClientMethodName = "CreateClient";
    private const string CatalogResourceValue = "catalog-api";
    private const string HttpClientFactoryExtensionsTypeName = "HttpClientFactoryServiceCollectionExtensions";
    private const string HttpClientTypeName = "HttpClient";
    private const string HttpClientFactoryTypeName = "IHttpClientFactory";
    private const string HttpRequestMessageTypeName = "HttpRequestMessage";
    private const string MicrosoftDependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";
    private const string RequestUriParameterName = "requestUri";
    private const string RequestUriPropertyName = "RequestUri";
    private const string SharedKernelBrandingNamespace = "SharedKernel.Branding";
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
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ProductionApiHttpRule);

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

            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, sourceApi),
                OperationKind.Invocation);
            compilationContext.RegisterOperationAction(
                operationContext => AnalyzeAssignment(operationContext, sourceApi),
                OperationKind.SimpleAssignment,
                OperationKind.CoalesceAssignment);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, ApiDestination sourceApi)
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

        if (IsHttpClientRequestInvocation(invocation))
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
                    allowBareResourceName: true);
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
                allowBareResourceName: true);
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
                allowBareResourceName: false);
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
            allowBareResourceName: false);

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
                allowBareResourceName: false);
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
        bool allowBareResourceName)
    {
        var destination = ResolveDestination(
            operation,
            new HashSet<ISymbol>(SymbolEqualityComparer.Default),
            sourceApi,
            allowBareResourceName);

        if (destination != ApiDestination.None)
        {
            destinations[destination] = location;
        }
    }

    private static ApiDestination ResolveDestination(
        IOperation operation,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        bool allowBareResourceName)
    {
        if (operation is IFieldReferenceOperation fieldReference)
        {
            var resourceNameDestination = GetResourceNameDestination(fieldReference.Field);
            if (resourceNameDestination != ApiDestination.None)
            {
                return resourceNameDestination;
            }
        }

        if (operation.ConstantValue is { HasValue: true, Value: string constantString })
        {
            var constantDestination = GetKnownValueDestination(constantString, allowBareResourceName);
            if (constantDestination != ApiDestination.None)
            {
                return constantDestination;
            }
        }

        return operation switch
        {
            IConversionOperation conversion => ResolveDestination(conversion.Operand, visitedSymbols, sourceApi, allowBareResourceName),
            IParenthesizedOperation parenthesized => ResolveDestination(parenthesized.Operand, visitedSymbols, sourceApi, allowBareResourceName),
            IArgumentOperation argument => ResolveDestination(argument.Value, visitedSymbols, sourceApi, allowBareResourceName),
            ILocalReferenceOperation localReference => ResolveLocalDestination(localReference, visitedSymbols, sourceApi, allowBareResourceName),
            IObjectCreationOperation objectCreation when IsUriType(objectCreation.Type) => ResolveFirstDestination(
                objectCreation.Arguments.Select(static argument => argument.Value),
                visitedSymbols,
                sourceApi,
                allowBareResourceName),
            IObjectCreationOperation objectCreation when IsHttpRequestMessageType(objectCreation.Type) => ResolveHttpRequestMessageDestination(
                objectCreation,
                visitedSymbols,
                sourceApi,
                allowBareResourceName),
            IInterpolatedStringOperation interpolatedString when allowBareResourceName => ResolveFirstDestination(
                interpolatedString.Parts,
                visitedSymbols,
                sourceApi,
                allowBareResourceName),
            IInterpolatedStringOperation interpolatedString => ResolveComposedUriDestination(
                interpolatedString,
                visitedSymbols,
                sourceApi),
            IInterpolationOperation interpolation => ResolveDestination(interpolation.Expression, visitedSymbols, sourceApi, allowBareResourceName),
            IBinaryOperation binary when !allowBareResourceName && binary.Type?.SpecialType == SpecialType.System_String => ResolveComposedUriDestination(
                binary,
                visitedSymbols,
                sourceApi),
            IBinaryOperation binary => ResolveFirstDestination([binary.LeftOperand, binary.RightOperand], visitedSymbols, sourceApi, allowBareResourceName),
            IConditionalOperation conditional => ResolveBranchDestination([conditional.WhenTrue, conditional.WhenFalse], visitedSymbols, sourceApi, allowBareResourceName),
            ICoalesceOperation coalesce => ResolveBranchDestination([coalesce.Value, coalesce.WhenNull], visitedSymbols, sourceApi, allowBareResourceName),
            _ => ApiDestination.None,
        };
    }

    private static ApiDestination ResolveFirstDestination(
        IEnumerable<IOperation?> operations,
        ISet<ISymbol> visitedSymbols,
        ApiDestination sourceApi,
        bool allowBareResourceName)
    {
        foreach (var operation in operations)
        {
            if (operation is null)
            {
                continue;
            }

            var destination = ResolveDestination(operation, visitedSymbols, sourceApi, allowBareResourceName);
            if (destination != ApiDestination.None)
            {
                return destination;
            }
        }

        return ApiDestination.None;
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

        if (operation is IInterpolatedStringTextOperation textOperation
            && textOperation.Text.ConstantValue is { HasValue: true, Value: string text })
        {
            candidate.Append(text);
        }
        else if (operation is IInterpolationOperation interpolation)
        {
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
                allowBareResourceName: true);
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
        bool allowBareResourceName)
    {
        var sourceDestination = ApiDestination.None;

        foreach (var operation in operations)
        {
            if (operation is null)
            {
                continue;
            }

            var destination = ResolveDestination(operation, visitedSymbols, sourceApi, allowBareResourceName);
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
        bool allowBareResourceName)
    {
        foreach (var argument in objectCreation.Arguments)
        {
            if (!IsRequestUriParameter(argument.Parameter))
            {
                continue;
            }

            var destination = ResolveDestination(argument.Value, visitedSymbols, sourceApi, allowBareResourceName);
            if (destination != ApiDestination.None)
            {
                return destination;
            }
        }

        if (objectCreation.Initializer is null)
        {
            return ApiDestination.None;
        }

        foreach (var operation in objectCreation.Initializer.ChildOperations)
        {
            if (operation is not IAssignmentOperation assignment || !IsHttpRequestUri(assignment.Target))
            {
                continue;
            }

            var destination = ResolveDestination(assignment.Value, visitedSymbols, sourceApi, allowBareResourceName);
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
        bool allowBareResourceName)
    {
        if (!visitedSymbols.Add(localReference.Local))
        {
            return ApiDestination.None;
        }

        var root = (IOperation)localReference;
        while (root.Parent is not null)
        {
            root = root.Parent;
        }

        IOperation? initializer = null;
        var hasReachingInitializer = TryFindReachingLocalInitializer(
            root,
            localReference.Local,
            localReference.Syntax.SpanStart,
            ref initializer);
        return !hasReachingInitializer || initializer is null
            ? ApiDestination.None
            : ResolveDestination(initializer, visitedSymbols, sourceApi, allowBareResourceName);
    }

    private static bool TryFindReachingLocalInitializer(
        IOperation operation,
        ILocalSymbol local,
        int usePosition,
        ref IOperation? initializer)
    {
        if (operation is IVariableDeclaratorOperation declarator
            && SymbolEqualityComparer.Default.Equals(declarator.Symbol, local))
        {
            initializer = declarator.Initializer?.Value;
        }
        else if (operation.Syntax.SpanStart < usePosition && IsWriteToLocal(operation, local))
        {
            return false;
        }

        foreach (var child in operation.ChildOperations)
        {
            if (!TryFindReachingLocalInitializer(child, local, usePosition, ref initializer))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWriteToLocal(IOperation operation, ILocalSymbol local)
    {
        return operation switch
        {
            IAssignmentOperation assignment => ContainsLocalReference(assignment.Target, local),
            IIncrementOrDecrementOperation incrementOrDecrement => ContainsLocalReference(incrementOrDecrement.Target, local),
            IArgumentOperation argument when argument.Parameter?.RefKind is RefKind.Ref or RefKind.Out => ContainsLocalReference(argument.Value, local),
            _ => false
        };
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

        return string.Equals(value, BrandingResourceValue, StringComparison.OrdinalIgnoreCase)
            ? ApiDestination.Branding
            : ApiDestination.None;
    }

    private static string GetResourceValue(ApiDestination destination)
    {
        return destination switch
        {
            ApiDestination.Admin => AdminResourceValue,
            ApiDestination.Catalog => CatalogResourceValue,
            ApiDestination.Branding => BrandingResourceValue,
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
            "BrandingApi" => ApiDestination.Branding,
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

        if (IsNamespaceOrChild(namespaceName, "ViajantesTurismo.Catalog.Contracts.Http"))
        {
            return ApiDestination.Catalog;
        }

        if (IsNamespaceOrChild(namespaceName, "ViajantesTurismo.Branding.Contracts.Http")
            || (string.Equals(namespaceName, SharedKernelBrandingNamespace, StringComparison.Ordinal)
                && type.Name is BrandingApiClientInterfaceTypeName or BrandingApiClientTypeName))
        {
            return ApiDestination.Branding;
        }

        return ApiDestination.None;
    }

    private static ApiDestination GetSourceApi(string? assemblyName)
    {
        return assemblyName switch
        {
            "ViajantesTurismo.Admin.ApiService" => ApiDestination.Admin,
            "ViajantesTurismo.Catalog.ApiService" => ApiDestination.Catalog,
            "ViajantesTurismo.Branding.ApiService" => ApiDestination.Branding,
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

    private static bool IsHttpClientRequestInvocation(IInvocationOperation invocation)
    {
        if (IsExactHttpClientType(invocation.TargetMethod.ContainingType))
        {
            return true;
        }

        var definition = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;
        return definition.IsExtensionMethod
            && definition.Parameters.Length > 0
            && IsHttpClientType(definition.Parameters[0].Type)
            && IsNamespaceOrChild(definition.ContainingNamespace.ToDisplayString(), SystemNetHttpNamespace);
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
            ApiDestination.Branding => "Branding API",
            _ => "Unknown API"
        };
    }

    private static string GetDestinationDisplayName(ApiDestination destination)
    {
        return destination switch
        {
            ApiDestination.Admin => "Admin API ('api')",
            ApiDestination.Catalog => "Catalog API ('catalog-api')",
            ApiDestination.Branding => "Branding API ('branding-api')",
            _ => "unknown API"
        };
    }

    private enum ApiDestination
    {
        None,
        Admin,
        Catalog,
        Branding
    }
}
