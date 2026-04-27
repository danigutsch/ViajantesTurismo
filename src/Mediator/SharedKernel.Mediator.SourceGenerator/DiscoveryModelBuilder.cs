using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Builds the aggregate discovery model for the initial report emitter.
/// </summary>
internal static class DiscoveryModelBuilder
{
    private static readonly DiagnosticDescriptor InaccessibleRegistrationTypeDescriptor = new(
        id: "SKMED010",
        title: "Mediator registration type is inaccessible",
        messageFormat: "Type '{0}' is inaccessible to generated mediator registrations",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateGeneratedRegistrationDescriptor = new(
        id: "SKMED012",
        title: "Generated mediator registration is duplicated",
        messageFormat: "Generated mediator registration '{0}' implemented by '{1}' is duplicated",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiscoveryModel Build(Compilation compilation, CancellationToken cancellationToken)
    {
        var discoverySymbols = DiscoverySymbols.Create(compilation);
        var moduleAttribute = compilation.GetTypeByMetadataName(MetadataNames.MediatorModuleAttribute);

        if (!discoverySymbols.IsComplete)
        {
            return new DiscoveryModel(
                [],
                [],
                [],
                [],
                []);
        }

        var discoveryState = new DiscoveryState();
        var modules = new List<ModuleDescriptor>();

        foreach (var assembly in EnumerateAssemblies(compilation, moduleAttribute))
        {
            modules.Add(
                new ModuleDescriptor(
                    assembly.Identity.Name,
                    SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly),
                    HasModuleMarker(assembly, moduleAttribute)));

            CollectTypes(
                assembly.GlobalNamespace,
                discoverySymbols,
                discoveryState,
                compilation.Assembly,
                cancellationToken);
        }

        return new DiscoveryModel(
            [.. modules.OrderBy(static module => module.AssemblyName, StringComparer.Ordinal)],
            BuildRequestDescriptors(discoveryState.RequestContracts, discoveryState.RequestHandlers, discoveryState.Pipelines),
            BuildNotificationDescriptors(discoveryState.NotificationContracts, discoveryState.NotificationHandlers),
            BuildStreamRequestDescriptors(discoveryState.StreamRequestContracts, discoveryState.StreamHandlers),
            [.. discoveryState.Diagnostics.OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start)]);
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
                     referencedAssembly => HasModuleMarker(referencedAssembly, moduleAttribute)))
        {
            yield return referencedAssembly;
        }
    }

    private static bool HasModuleMarker(IAssemblySymbol assembly, INamedTypeSymbol? moduleAttribute)
    {
        return moduleAttribute is not null
               && assembly.GetAttributes().Any(
                   attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, moduleAttribute));
    }

    private static void CollectTypes(
        INamespaceSymbol @namespace,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            CollectTypes(
                nestedNamespace,
                discoverySymbols,
                discoveryState,
                primaryAssembly,
                cancellationToken);
        }

        foreach (var type in @namespace.GetTypeMembers())
        {
            CollectType(
                type,
                discoverySymbols,
                discoveryState,
                primaryAssembly,
                cancellationToken);
        }
    }

    private static void CollectType(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (TryCreateRequestContract(type, discoverySymbols, out var requestContract))
        {
            discoveryState.RequestContracts[requestContract.MetadataName] = requestContract;
        }

        foreach (var requestHandler in CreateRequestHandlers(type, discoverySymbols, discoveryState, primaryAssembly))
        {
            discoveryState.RequestHandlers.Add(requestHandler);
        }

        foreach (var pipeline in CreatePipelines(type, discoverySymbols, discoveryState, primaryAssembly))
        {
            discoveryState.Pipelines.Add(pipeline);
        }

        if (TryCreateNotificationContract(type, discoverySymbols, out var notificationMetadataName))
        {
            discoveryState.NotificationContracts[notificationMetadataName] = notificationMetadataName;
        }

        foreach (var notificationHandler in CreateNotificationHandlers(type, discoverySymbols, discoveryState, primaryAssembly))
        {
            discoveryState.NotificationHandlers.Add(notificationHandler);
        }

        if (TryCreateStreamRequestContract(type, discoverySymbols, out var streamRequestMetadataName, out var streamItemResponse))
        {
            discoveryState.StreamRequestContracts[streamRequestMetadataName] = streamItemResponse;
        }

        foreach (var streamHandler in CreateStreamHandlers(type, discoverySymbols, discoveryState, primaryAssembly))
        {
            discoveryState.StreamHandlers.Add(streamHandler);
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            CollectType(
                nestedType,
                discoverySymbols,
                discoveryState,
                primaryAssembly,
                cancellationToken);
        }
    }

    private static ImmutableArray<RequestDescriptor> BuildRequestDescriptors(
        IDictionary<string, RawRequestContract> requestContracts,
        IEnumerable<HandlerDescriptor> requestHandlers,
        IEnumerable<PipelineDescriptor> pipelines)
    {
        var handlersByRequest = requestHandlers
            .GroupBy(static handler => handler.RequestMetadataName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static handler => handler.MetadataName, StringComparer.Ordinal)
                    .ThenBy(static handler => handler.Kind)
                    .ToImmutableArray(),
                StringComparer.Ordinal);

        var pipelinesByRequest = pipelines
            .GroupBy(static pipeline => pipeline.RequestMetadataName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static pipeline => pipeline.Stage)
                    .ThenBy(static pipeline => pipeline.Order)
                    .ThenBy(static pipeline => pipeline.MetadataName, StringComparer.Ordinal)
                    .ToImmutableArray(),
                StringComparer.Ordinal);

        return [.. requestContracts.Values
            .OrderBy(static request => request.MetadataName, StringComparer.Ordinal)
            .Select(
                request =>
                {
                    var handlers = handlersByRequest.TryGetValue(request.MetadataName, out var requestHandlers)
                        ? requestHandlers
                        : [];
                    var requestPipelines = pipelinesByRequest.TryGetValue(request.MetadataName, out var discoveredPipelines)
                        ? discoveredPipelines
                        : [];

                    return new RequestDescriptor(
                        request.MetadataName,
                        request.Namespace,
                        request.Name,
                        request.Kind,
                        request.Response,
                        request.IsValueType,
                        handlers,
                        requestPipelines);
                })];
    }

    private static ImmutableArray<NotificationDescriptor> BuildNotificationDescriptors(
        IDictionary<string, string> notificationContracts,
        IEnumerable<NotificationHandlerDescriptor> notificationHandlers)
    {
        var handlersByNotification = notificationHandlers
            .GroupBy(static handler => handler.NotificationMetadataName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static handler => handler.MetadataName, StringComparer.Ordinal)
                    .ToImmutableArray(),
                StringComparer.Ordinal);

        return [.. notificationContracts.Keys
            .OrderBy(static notification => notification, StringComparer.Ordinal)
            .Select(
                notification =>
                {
                    var handlers = handlersByNotification.TryGetValue(notification, out var notificationHandlersForContract)
                        ? notificationHandlersForContract
                        : [];

                    return new NotificationDescriptor(notification, handlers);
                })];
    }

    private static ImmutableArray<StreamRequestDescriptor> BuildStreamRequestDescriptors(
        IDictionary<string, ResponseDescriptor> streamRequestContracts,
        IEnumerable<StreamHandlerDescriptor> streamHandlers)
    {
        var handlersByRequest = streamHandlers
            .GroupBy(static handler => handler.RequestMetadataName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group
                    .OrderBy(static handler => handler.MetadataName, StringComparer.Ordinal)
                    .ToImmutableArray(),
                StringComparer.Ordinal);

        return [.. streamRequestContracts
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(
                pair =>
                {
                    var handlers = handlersByRequest.TryGetValue(pair.Key, out var streamHandlersForRequest)
                        ? streamHandlersForRequest
                        : [];

                    return new StreamRequestDescriptor(pair.Key, pair.Value, handlers);
                })];
    }

    private static bool TryCreateRequestContract(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        out RawRequestContract requestContract)
    {
        requestContract = null!;

        if (!IsDiscoverableType(type))
        {
            return false;
        }

        var requestInterface = FindBestRequestInterface(type, discoverySymbols);
        if (requestInterface is null)
        {
            return false;
        }

        var responseType = requestInterface.TypeArguments[0];
        requestContract = new RawRequestContract(
            GetMetadataName(type),
            GetNamespace(type),
            type.Name,
            GetRequestKind(type, discoverySymbols),
            CreateResponseDescriptor(responseType),
            type.IsValueType);

        return true;
    }

    private static IEnumerable<HandlerDescriptor> CreateRequestHandlers(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly)
    {
        if (!IsDiscoverableType(type))
        {
            yield break;
        }

        var handlerInterfaces = type.AllInterfaces
            .Select(candidate => CreateRequestHandlerDescriptor(type, candidate, discoverySymbols, discoveryState, primaryAssembly))
            .OfType<(string Key, int Priority, HandlerDescriptor Descriptor)>()
            .GroupBy(static descriptor => descriptor.Key, StringComparer.Ordinal)
            .Select(static group => group.OrderBy(static item => item.Priority).First().Descriptor)
            .OrderBy(static descriptor => descriptor.MetadataName, StringComparer.Ordinal);

        foreach (var handlerInterface in handlerInterfaces)
        {
            yield return handlerInterface;
        }
    }

    private static (string Key, int Priority, HandlerDescriptor Descriptor)? CreateRequestHandlerDescriptor(
        INamedTypeSymbol type,
        INamedTypeSymbol candidate,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly)
    {
        var originalDefinition = candidate.OriginalDefinition;
        HandlerKind? handlerKind = null;
        var priority = 0;

        if (SymbolEqualityComparer.Default.Equals(originalDefinition, discoverySymbols.QueryHandlerInterface))
        {
            handlerKind = HandlerKind.Query;
            priority = 0;
        }
        else if (SymbolEqualityComparer.Default.Equals(originalDefinition, discoverySymbols.CommandHandlerOfResponseInterface))
        {
            handlerKind = HandlerKind.CommandWithResponse;
            priority = 1;
        }
        else if (SymbolEqualityComparer.Default.Equals(originalDefinition, discoverySymbols.CommandHandlerInterface))
        {
            handlerKind = HandlerKind.Command;
            priority = 2;
        }
        else if (SymbolEqualityComparer.Default.Equals(originalDefinition, discoverySymbols.HandlerInterface))
        {
            handlerKind = HandlerKind.Request;
            priority = 3;
        }

        if (handlerKind is null)
        {
            return null;
        }

        var requestType = candidate.TypeArguments[0];
        var responseType = handlerKind is HandlerKind.Command
            ? discoverySymbols.UnitType
            : candidate.TypeArguments[1];
        var isAccessibleToGeneratedMediator = IsAccessibleToGeneratedMediator(type, primaryAssembly);
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, discoveryState);
        var descriptor = new HandlerDescriptor(
            GetMetadataName(type),
            GetNamespace(type),
            type.Name,
            GetTypeDisplayString(requestType),
            GetTypeDisplayString(responseType),
            GetHandleMethodName(type),
            type.DeclaredAccessibility,
            isAccessibleToGeneratedMediator,
            handlerKind.Value);

        if (isAccessibleToGeneratedMediator)
        {
            ReportDuplicateGeneratedRegistration(
                type,
                GetSelfRegistrationServiceType(descriptor.MetadataName),
                descriptor.MetadataName,
                discoveryState);
            ReportDuplicateGeneratedRegistration(
                type,
                GetHandlerServiceType(descriptor),
                descriptor.MetadataName,
                discoveryState);
        }

        return ($"{descriptor.RequestMetadataName}|{descriptor.ResponseMetadataName}", priority, descriptor);
    }

    private static IEnumerable<PipelineDescriptor> CreatePipelines(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly)
    {
        if (!IsDiscoverableType(type))
        {
            yield break;
        }

        var isAccessibleToGeneratedMediator = IsAccessibleToGeneratedMediator(type, primaryAssembly);
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, discoveryState);

        var pipelineTypeArguments = type.AllInterfaces
            .Where(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.PipelineInterface))
            .Select(static candidate => candidate.TypeArguments);

        foreach (var typeArguments in pipelineTypeArguments)
        {
            var pipelineOrder = type.GetAttributes().SingleOrDefault(
                attribute => SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    discoverySymbols.PipelineOrderAttribute));

            var stage = pipelineOrder?.ConstructorArguments[0].Value is int stageValue ? stageValue : 0;
            var order = pipelineOrder?.NamedArguments.FirstOrDefault(
                static argument => argument.Key == "Order").Value.Value is int orderValue
                ? orderValue
                : 0;

            yield return new PipelineDescriptor(
                GetMetadataName(type),
                type.TypeParameters.Length > 0 ? GetMetadataName(type.OriginalDefinition) : null,
                GetTypeDisplayString(typeArguments[0]),
                stage,
                order,
                type.TypeParameters.Length > 0 ? PipelineApplicability.OpenGeneric : PipelineApplicability.Closed,
                isAccessibleToGeneratedMediator);

            if (isAccessibleToGeneratedMediator)
            {
                var implementationType = GetMetadataName(type);
                ReportDuplicateGeneratedRegistration(
                    type,
                    GetSelfRegistrationServiceType(implementationType),
                    implementationType,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.IPipelineBehavior<{GetTypeDisplayString(typeArguments[0])}, {GetTypeDisplayString(typeArguments[1])}>",
                    implementationType,
                    discoveryState);
            }
        }
    }

    private static bool TryCreateNotificationContract(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        out string notificationMetadataName)
    {
        notificationMetadataName = string.Empty;

        if (!IsDiscoverableType(type) || !ImplementsInterface(type, discoverySymbols.NotificationInterface))
        {
            return false;
        }

        notificationMetadataName = GetMetadataName(type);
        return true;
    }

    private static IEnumerable<NotificationHandlerDescriptor> CreateNotificationHandlers(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly)
    {
        if (!IsDiscoverableType(type))
        {
            yield break;
        }

        var isAccessibleToGeneratedMediator = IsAccessibleToGeneratedMediator(type, primaryAssembly);
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, discoveryState);

        var notificationTypeArguments = type.AllInterfaces
            .Where(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.NotificationHandlerInterface))
            .Select(static candidate => candidate.TypeArguments);

        foreach (var typeArguments in notificationTypeArguments)
        {
            yield return new NotificationHandlerDescriptor(
                GetMetadataName(type),
                GetNamespace(type),
                type.Name,
                GetTypeDisplayString(typeArguments[0]),
                GetHandleMethodName(type),
                type.DeclaredAccessibility,
                isAccessibleToGeneratedMediator);

            if (isAccessibleToGeneratedMediator)
            {
                var implementationType = GetMetadataName(type);
                ReportDuplicateGeneratedRegistration(
                    type,
                    GetSelfRegistrationServiceType(implementationType),
                    implementationType,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.INotificationHandler<{GetTypeDisplayString(typeArguments[0])}>",
                    implementationType,
                    discoveryState);
            }
        }
    }

    private static bool TryCreateStreamRequestContract(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        out string requestMetadataName,
        out ResponseDescriptor itemResponse)
    {
        requestMetadataName = string.Empty;
        itemResponse = default!;

        if (!IsDiscoverableType(type))
        {
            return false;
        }

        var streamRequestInterface = type.AllInterfaces.SingleOrDefault(
            candidate => SymbolEqualityComparer.Default.Equals(
                candidate.OriginalDefinition,
                discoverySymbols.StreamRequestInterface));

        if (streamRequestInterface is null)
        {
            return false;
        }

        requestMetadataName = GetMetadataName(type);
        itemResponse = CreateResponseDescriptor(streamRequestInterface.TypeArguments[0]);
        return true;
    }

    private static IEnumerable<StreamHandlerDescriptor> CreateStreamHandlers(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        DiscoveryState discoveryState,
        IAssemblySymbol primaryAssembly)
    {
        if (!IsDiscoverableType(type))
        {
            yield break;
        }

        var isAccessibleToGeneratedMediator = IsAccessibleToGeneratedMediator(type, primaryAssembly);
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, discoveryState);

        var streamHandlerTypeArguments = type.AllInterfaces
            .Where(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.StreamHandlerInterface))
            .Select(static candidate => candidate.TypeArguments);

        foreach (var typeArguments in streamHandlerTypeArguments)
        {
            yield return new StreamHandlerDescriptor(
                GetMetadataName(type),
                GetNamespace(type),
                type.Name,
                GetTypeDisplayString(typeArguments[0]),
                GetTypeDisplayString(typeArguments[1]),
                GetHandleMethodName(type),
                type.DeclaredAccessibility,
                isAccessibleToGeneratedMediator);

            if (isAccessibleToGeneratedMediator)
            {
                var implementationType = GetMetadataName(type);
                ReportDuplicateGeneratedRegistration(
                    type,
                    GetSelfRegistrationServiceType(implementationType),
                    implementationType,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.IStreamRequestHandler<{GetTypeDisplayString(typeArguments[0])}, {GetTypeDisplayString(typeArguments[1])}>",
                    implementationType,
                    discoveryState);
            }
        }
    }

    private static bool IsAccessibleToGeneratedMediator(INamedTypeSymbol type, IAssemblySymbol primaryAssembly)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    continue;
                case Accessibility.Internal:
                case Accessibility.ProtectedOrInternal:
                    if (SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, primaryAssembly))
                    {
                        continue;
                    }

                    return false;
                default:
                    return false;
            }
        }

        return true;
    }

    private static void ReportInaccessibleRegistrationType(
        INamedTypeSymbol type,
        bool isAccessibleToGeneratedMediator,
        DiscoveryState discoveryState)
    {
        if (isAccessibleToGeneratedMediator)
        {
            return;
        }

        var metadataName = GetMetadataName(type);
        if (!discoveryState.DiagnosedInaccessibleTypes.Add(metadataName))
        {
            return;
        }

        var location = type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? type.Locations.FirstOrDefault();
        if (location is null)
        {
            return;
        }

        discoveryState.Diagnostics.Add(Diagnostic.Create(InaccessibleRegistrationTypeDescriptor, location, metadataName));
    }

    private static void ReportDuplicateGeneratedRegistration(
        INamedTypeSymbol type,
        string serviceType,
        string implementationType,
        DiscoveryState discoveryState)
    {
        var registrationKey = $"{serviceType}|{implementationType}";
        if (discoveryState.GeneratedRegistrationKeys.Add(registrationKey))
        {
            return;
        }

        if (!discoveryState.DiagnosedDuplicateRegistrationKeys.Add(registrationKey))
        {
            return;
        }

        var location = type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? type.Locations.FirstOrDefault();
        if (location is null)
        {
            return;
        }

        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                DuplicateGeneratedRegistrationDescriptor,
                location,
                serviceType,
                implementationType));
    }

    private static string GetSelfRegistrationServiceType(string implementationType)
    {
        return implementationType;
    }

    private static string GetHandlerServiceType(HandlerDescriptor handler)
    {
        return handler.Kind switch
        {
            HandlerKind.Request => $"global::SharedKernel.Mediator.IRequestHandler<{handler.RequestMetadataName}, {handler.ResponseMetadataName}>",
            HandlerKind.Command => $"global::SharedKernel.Mediator.ICommandHandler<{handler.RequestMetadataName}>",
            HandlerKind.CommandWithResponse => $"global::SharedKernel.Mediator.ICommandHandler<{handler.RequestMetadataName}, {handler.ResponseMetadataName}>",
            HandlerKind.Query => $"global::SharedKernel.Mediator.IQueryHandler<{handler.RequestMetadataName}, {handler.ResponseMetadataName}>",
            _ => throw new ArgumentOutOfRangeException(nameof(handler))
        };
    }

    private static bool IsDiscoverableType(INamedTypeSymbol type)
    {
        return type.TypeKind is not TypeKind.Interface
               && !type.IsAbstract
               && !type.IsImplicitlyDeclared;
    }

    private static INamedTypeSymbol? FindBestRequestInterface(INamedTypeSymbol type, DiscoverySymbols discoverySymbols)
    {
        if (type.AllInterfaces.SingleOrDefault(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.QueryInterface)) is { } queryInterface)
        {
            return queryInterface;
        }

        if (type.AllInterfaces.SingleOrDefault(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.CommandOfResponseInterface)) is { } commandOfResponseInterface)
        {
            return commandOfResponseInterface;
        }

        if (ImplementsInterface(type, discoverySymbols.CommandInterface))
        {
            return type.AllInterfaces.Single(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.RequestInterface));
        }

        return type.AllInterfaces.SingleOrDefault(
            candidate => SymbolEqualityComparer.Default.Equals(
                candidate.OriginalDefinition,
                discoverySymbols.RequestInterface));
    }

    private static RequestKind GetRequestKind(INamedTypeSymbol type, DiscoverySymbols discoverySymbols)
    {
        if (ImplementsInterface(type, discoverySymbols.QueryInterface))
        {
            return RequestKind.Query;
        }

        if (ImplementsInterface(type, discoverySymbols.CommandOfResponseInterface))
        {
            return RequestKind.CommandWithResponse;
        }

        return ImplementsInterface(type, discoverySymbols.CommandInterface)
            ? RequestKind.Command
            : RequestKind.Request;
    }

    private static bool ImplementsInterface(INamedTypeSymbol type, INamedTypeSymbol interfaceSymbol)
    {
        return type.AllInterfaces.Any(
            candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, interfaceSymbol)
                         || SymbolEqualityComparer.Default.Equals(candidate, interfaceSymbol));
    }

    private static ResponseDescriptor CreateResponseDescriptor(ITypeSymbol type)
    {
        var namedType = type as INamedTypeSymbol;
        var isConstructedGenericType = namedType is { IsGenericType: true, TypeArguments.Length: > 0 };

        return new ResponseDescriptor(
            GetTypeDisplayString(type),
            isConstructedGenericType,
            isConstructedGenericType ? GetMetadataName(namedType!.OriginalDefinition) : null,
            isConstructedGenericType
                ? [.. namedType!.TypeArguments.Select(GetTypeDisplayString)]
                : [],
            [.. type.AllInterfaces
                .Select(GetTypeDisplayString)
                .OrderBy(static interfaceName => interfaceName, StringComparer.Ordinal)]);
    }

    private static string GetMetadataName(INamedTypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetNamespace(INamedTypeSymbol type)
    {
        return type.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : type.ContainingNamespace.ToDisplayString();
    }

    private static string GetTypeDisplayString(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static string GetHandleMethodName(INamedTypeSymbol type)
    {
        return type.GetMembers()
                   .OfType<IMethodSymbol>()
                   .FirstOrDefault(static method => method.Name == "Handle")?
                   .Name
               ?? "Handle";
    }

}
