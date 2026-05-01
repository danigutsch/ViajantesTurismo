using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Builds the aggregate discovery model for the initial report emitter.
/// </summary>
internal static class DiscoveryModelBuilder
{
    private const string PrimaryAssemblyNamePropertyName = "PrimaryAssemblyName";

    private static readonly DiagnosticDescriptor MissingHandlerDescriptor = new(
        id: MediatorDiagnosticIds.MissingHandler,
        title: "Request has no handler",
        messageFormat: "Request '{0}' does not have an accessible compatible handler",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleHandlersDescriptor = new(
        id: MediatorDiagnosticIds.MultipleHandlers,
        title: "Request has multiple handlers",
        messageFormat: "Request '{0}' has {1} accessible compatible handlers",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidHandlerSignatureDescriptor = new(
        id: MediatorDiagnosticIds.InvalidHandlerSignature,
        title: "Handler has invalid signature",
        messageFormat: "Handler '{0}' does not expose a compatible Handle method for request '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InaccessibleRegistrationTypeDescriptor = new(
        id: MediatorDiagnosticIds.InaccessibleRegistrationType,
        title: "Mediator registration type is inaccessible",
        messageFormat: "Type '{0}' is inaccessible to generated mediator registrations",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingModuleMarkerDescriptor = new(
        id: MediatorDiagnosticIds.MissingModuleMarker,
        title: "Handler module is not marked with [assembly: MediatorModule]",
        messageFormat: "Assembly '{0}' contains mediator registrations but is not marked with [assembly: MediatorModule]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateGeneratedRegistrationDescriptor = new(
        id: MediatorDiagnosticIds.DuplicateGeneratedRegistration,
        title: "Generated mediator registration is duplicated",
        messageFormat: "Generated mediator registration '{0}' implemented by '{1}' is duplicated",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnprovenObjectDispatchCoverageDescriptor = new(
        id: MediatorDiagnosticIds.UnprovenObjectDispatchCoverage,
        title: "Generated object dispatch coverage cannot be proven",
        messageFormat: "Generated object dispatch coverage cannot be proven because assembly '{0}' is not marked with [assembly: MediatorModule]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NotificationHandlersRequireExplicitOrderDescriptor = new(
        id: MediatorDiagnosticIds.NotificationHandlersRequireExplicitOrder,
        title: "Notification handlers require explicit order",
        messageFormat: "Notification '{0}' has multiple sequential handlers and handler '{1}' does not declare [NotificationOrder]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateNotificationHandlerOrderDescriptor = new(
        id: MediatorDiagnosticIds.DuplicateNotificationHandlerOrder,
        title: "Duplicate notification handler order",
        messageFormat: "Notification '{0}' has multiple handlers with explicit order {1}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidPipelineGenericArityDescriptor = new(
        id: MediatorDiagnosticIds.InvalidPipelineGenericArity,
        title: "Pipeline behavior has invalid generic arity",
        messageFormat: "Pipeline behavior '{0}' has invalid generic arity {1}; open generic pipeline behaviors must declare exactly two type parameters",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicatePipelineOrderDescriptor = new(
        id: MediatorDiagnosticIds.DuplicatePipelineOrder,
        title: "Duplicate pipeline order",
        messageFormat: "Request '{0}' has multiple applicable pipeline behaviors with stage {1} and order {2}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NeverAppliesPipelineDescriptor = new(
        id: MediatorDiagnosticIds.NeverAppliesPipeline,
        title: "Pipeline behavior is registered but never applies",
        messageFormat: "Pipeline behavior '{0}' is registered but does not apply to any discovered request",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnboundPipelineConstraintsDescriptor = new(
        id: MediatorDiagnosticIds.UnboundPipelineConstraints,
        title: "Pipeline behavior constraints cannot bind to any request",
        messageFormat: "Pipeline behavior '{0}' constraints cannot bind to any discovered request/response pair",
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

        ReportUnmarkedReferencedAssemblies(compilation, discoverySymbols, moduleAttribute, discoveryState, cancellationToken);

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

        var requests = BuildRequestDescriptors(
            discoveryState.RequestContracts,
            discoveryState.RequestHandlers,
            discoveryState.Pipelines,
            compilation.Assembly,
            discoveryState);
        ReportRequestHandlerDiagnostics(requests, discoveryState);

        return new DiscoveryModel(
            [.. modules.OrderBy(static module => module.AssemblyName, StringComparer.Ordinal)],
            requests,
            BuildNotificationDescriptors(discoveryState.NotificationContracts, discoveryState.NotificationHandlers, discoveryState),
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

        if (TryCreateRequestContract(type, discoverySymbols, primaryAssembly, out var requestContract))
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
        IEnumerable<RawPipelineDescriptor> pipelines,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
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

        var matchedPipelinesByRequest = requestContracts.Keys.ToDictionary(
            static requestMetadataName => requestMetadataName,
            static _ => new List<(PipelineDescriptor Descriptor, RawPipelineDescriptor Raw)>(),
            StringComparer.Ordinal);

        foreach (var pipeline in pipelines)
        {
            foreach (var match in MatchPipelineDescriptors(pipeline, requestContracts.Values, primaryAssembly, discoveryState))
            {
                if (matchedPipelinesByRequest.TryGetValue(match.Descriptor.RequestMetadataName, out var requestPipelines))
                {
                    requestPipelines.Add(match);
                }
            }
        }

        foreach (var requestPipelines in matchedPipelinesByRequest)
        {
            foreach (var duplicateGroup in requestPipelines.Value
                         .GroupBy(static pipeline => (pipeline.Descriptor.Stage, pipeline.Descriptor.Order))
                         .Where(static group => group.Count() > 1))
            {
                foreach (var pipeline in duplicateGroup)
                {
                    discoveryState.Diagnostics.Add(
                        Diagnostic.Create(
                            DuplicatePipelineOrderDescriptor,
                            GetDiagnosticLocation(pipeline.Raw.TypeSymbol, primaryAssembly),
                            requestPipelines.Key,
                            duplicateGroup.Key.Stage,
                            duplicateGroup.Key.Order));
                }
            }
        }

        return [.. requestContracts.Values
            .OrderBy(static request => request.MetadataName, StringComparer.Ordinal)
            .Select(
                request =>
                {
                    var handlers = handlersByRequest.TryGetValue(request.MetadataName, out var requestHandlers)
                        ? requestHandlers
                        : [];
                    ImmutableArray<PipelineDescriptor> requestPipelines = matchedPipelinesByRequest.TryGetValue(
                        request.MetadataName,
                        out var discoveredPipelines)
                        ? [.. discoveredPipelines
                            .OrderBy(static pipeline => pipeline.Descriptor.Stage)
                            .ThenBy(static pipeline => pipeline.Descriptor.Order)
                            .ThenBy(static pipeline => pipeline.Descriptor.MetadataName, StringComparer.Ordinal)
                            .Select(static pipeline => pipeline.Descriptor)]
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

    private static IEnumerable<(PipelineDescriptor Descriptor, RawPipelineDescriptor Raw)> MatchPipelineDescriptors(
        RawPipelineDescriptor pipeline,
        IEnumerable<RawRequestContract> requestContracts,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
    {
        if (!pipeline.IsAccessibleToGeneratedMediator)
        {
            yield break;
        }

        if (pipeline.Applicability == PipelineApplicability.Closed)
        {
            var matchedRequest = requestContracts.FirstOrDefault(
                request =>
                    string.Equals(request.MetadataName, pipeline.RequestMetadataName, StringComparison.Ordinal)
                    && string.Equals(request.Response.MetadataName, pipeline.ResponseMetadataName, StringComparison.Ordinal));

            if (matchedRequest is null)
            {
                ReportNeverAppliesPipeline(pipeline, primaryAssembly, discoveryState);
                yield break;
            }

            yield return (
                new PipelineDescriptor(
                    pipeline.MetadataName,
                    pipeline.OpenGenericMetadataName,
                    matchedRequest.MetadataName,
                    pipeline.Stage,
                    pipeline.Order,
                    PipelineApplicability.Closed,
                    pipeline.IsAccessibleToGeneratedMediator),
                pipeline);
            yield break;
        }

        if (pipeline.TypeParameters.Length != 2)
        {
            ReportInvalidPipelineGenericArity(pipeline, primaryAssembly, discoveryState);
            yield break;
        }

        var matchedAny = false;
        var constraintFailure = false;

        foreach (var request in requestContracts)
        {
            if (TryBindOpenGenericPipeline(pipeline, request, out var descriptor, out var failedConstraints))
            {
                matchedAny = true;
                yield return (descriptor, pipeline);
                continue;
            }

            constraintFailure |= failedConstraints;
        }

        if (matchedAny)
        {
            yield break;
        }

        if (constraintFailure)
        {
            ReportUnboundPipelineConstraints(pipeline, primaryAssembly, discoveryState);
            yield break;
        }

        ReportNeverAppliesPipeline(pipeline, primaryAssembly, discoveryState);
    }

    private static bool TryBindOpenGenericPipeline(
        RawPipelineDescriptor pipeline,
        RawRequestContract request,
        out PipelineDescriptor descriptor,
        out bool failedConstraints)
    {
        descriptor = null!;
        failedConstraints = false;

        var bindings = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        if (!TryUnifyTypePattern(pipeline.RequestTypePattern, request.TypeSymbol, bindings)
            || !TryUnifyTypePattern(pipeline.ResponseTypePattern, request.ResponseTypeSymbol, bindings))
        {
            return false;
        }

        if (pipeline.TypeParameters.Any(typeParameter => !bindings.ContainsKey(typeParameter))
            || !TypeArgumentsSatisfyConstraints(pipeline.TypeParameters, bindings))
        {
            failedConstraints = true;
            return false;
        }

        var constructedType = pipeline.TypeSymbol.OriginalDefinition.Construct(
            [.. pipeline.TypeParameters.Select(typeParameter => bindings[typeParameter])]);
        descriptor = new PipelineDescriptor(
            GetMetadataName(constructedType),
            pipeline.OpenGenericMetadataName,
            request.MetadataName,
            pipeline.Stage,
            pipeline.Order,
            PipelineApplicability.OpenGeneric,
            pipeline.IsAccessibleToGeneratedMediator);
        return true;
    }

    private static bool TryUnifyTypePattern(
        ITypeSymbol pattern,
        ITypeSymbol actual,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings)
    {
        if (pattern is ITypeParameterSymbol typeParameter)
        {
            if (bindings.TryGetValue(typeParameter, out var boundType))
            {
                return SymbolEqualityComparer.Default.Equals(boundType, actual);
            }

            bindings[typeParameter] = actual;
            return true;
        }

        if (pattern is INamedTypeSymbol namedPattern)
        {
            if (actual is not INamedTypeSymbol namedActual
                || !SymbolEqualityComparer.Default.Equals(namedPattern.OriginalDefinition, namedActual.OriginalDefinition)
                || namedPattern.TypeArguments.Length != namedActual.TypeArguments.Length)
            {
                return false;
            }

            for (var index = 0; index < namedPattern.TypeArguments.Length; index++)
            {
                if (!TryUnifyTypePattern(namedPattern.TypeArguments[index], namedActual.TypeArguments[index], bindings))
                {
                    return false;
                }
            }

            return true;
        }

        return SymbolEqualityComparer.Default.Equals(pattern, actual);
    }

    private static bool TypeArgumentsSatisfyConstraints(
        ImmutableArray<ITypeParameterSymbol> typeParameters,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings)
    {
        foreach (var typeParameter in typeParameters)
        {
            var actualType = bindings[typeParameter];
            if (!TypeArgumentSatisfiesConstraints(actualType, typeParameter, bindings))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TypeArgumentSatisfiesConstraints(
        ITypeSymbol actualType,
        ITypeParameterSymbol typeParameter,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings)
    {
        if (typeParameter.HasReferenceTypeConstraint && !actualType.IsReferenceType)
        {
            return false;
        }

        if (typeParameter.HasValueTypeConstraint && !actualType.IsValueType)
        {
            return false;
        }

        if (typeParameter.HasUnmanagedTypeConstraint && !actualType.IsUnmanagedType)
        {
            return false;
        }

        if (typeParameter.HasNotNullConstraint && actualType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return false;
        }

        if (typeParameter.HasConstructorConstraint
            && actualType is INamedTypeSymbol namedActual
            && !namedActual.IsValueType
            && !namedActual.InstanceConstructors.Any(static constructor => constructor.Parameters.Length == 0 && constructor.DeclaredAccessibility == Accessibility.Public))
        {
            return false;
        }

        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            var substitutedConstraint = SubstituteTypeParameters(constraintType, bindings);
            if (substitutedConstraint is null || !ImplementsOrDerivesFrom(actualType, substitutedConstraint))
            {
                return false;
            }
        }

        return true;
    }

    private static ITypeSymbol? SubstituteTypeParameters(
        ITypeSymbol type,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> bindings)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return bindings.TryGetValue(typeParameter, out var boundType) ? boundType : null;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var substitutedArguments = new ITypeSymbol[namedType.TypeArguments.Length];
            for (var index = 0; index < namedType.TypeArguments.Length; index++)
            {
                var substitutedArgument = SubstituteTypeParameters(namedType.TypeArguments[index], bindings);
                if (substitutedArgument is null)
                {
                    return null;
                }

                substitutedArguments[index] = substitutedArgument;
            }

            return namedType.OriginalDefinition.Construct(substitutedArguments);
        }

        return type;
    }

    private static bool ImplementsOrDerivesFrom(ITypeSymbol actualType, ITypeSymbol constraintType)
    {
        if (SymbolEqualityComparer.Default.Equals(actualType, constraintType))
        {
            return true;
        }

        if (actualType is INamedTypeSymbol namedActual)
        {
            if (namedActual.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate, constraintType)))
            {
                return true;
            }

            for (var baseType = namedActual.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, constraintType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ReportInvalidPipelineGenericArity(
        RawPipelineDescriptor pipeline,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
    {
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                InvalidPipelineGenericArityDescriptor,
                GetDiagnosticLocation(pipeline.TypeSymbol, primaryAssembly),
                pipeline.MetadataName,
                pipeline.TypeParameters.Length));
    }

    private static void ReportNeverAppliesPipeline(
        RawPipelineDescriptor pipeline,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
    {
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                NeverAppliesPipelineDescriptor,
                GetDiagnosticLocation(pipeline.TypeSymbol, primaryAssembly),
                pipeline.MetadataName));
    }

    private static void ReportUnboundPipelineConstraints(
        RawPipelineDescriptor pipeline,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
    {
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                UnboundPipelineConstraintsDescriptor,
                GetDiagnosticLocation(pipeline.TypeSymbol, primaryAssembly),
                pipeline.MetadataName));
    }

    private static void ReportNotificationHandlersRequireExplicitOrder(
        string notificationMetadataName,
        NotificationHandlerDescriptor handler,
        DiscoveryState discoveryState)
    {
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                NotificationHandlersRequireExplicitOrderDescriptor,
                handler.DiagnosticLocation ?? Location.None,
                notificationMetadataName,
                handler.MetadataName));
    }

    private static void ReportDuplicateNotificationHandlerOrder(
        string notificationMetadataName,
        NotificationHandlerDescriptor handler,
        int order,
        DiscoveryState discoveryState)
    {
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                DuplicateNotificationHandlerOrderDescriptor,
                handler.DiagnosticLocation ?? Location.None,
                notificationMetadataName,
                order));
    }

    private static ImmutableArray<NotificationDescriptor> BuildNotificationDescriptors(
        IDictionary<string, string> notificationContracts,
        IEnumerable<NotificationHandlerDescriptor> notificationHandlers,
        DiscoveryState discoveryState)
    {
        var handlersByNotification = notificationHandlers
            .GroupBy(static handler => handler.NotificationMetadataName, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                group => OrderNotificationHandlers(group.Key, [.. group], discoveryState),
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

    private static ImmutableArray<NotificationHandlerDescriptor> OrderNotificationHandlers(
        string notificationMetadataName,
        ImmutableArray<NotificationHandlerDescriptor> handlers,
        DiscoveryState discoveryState)
    {
        var accessibleHandlers = handlers
            .Where(static handler => handler.IsAccessibleToGeneratedMediator)
            .ToImmutableArray();

        if (accessibleHandlers.Length > 1)
        {
            foreach (var unorderedHandler in accessibleHandlers.Where(static handler => !handler.HasExplicitOrder))
            {
                ReportNotificationHandlersRequireExplicitOrder(notificationMetadataName, unorderedHandler, discoveryState);
            }
        }

        foreach (var duplicateGroup in accessibleHandlers
                     .Where(static handler => handler.HasExplicitOrder)
                     .GroupBy(static handler => handler.Order)
                     .Where(static group => group.Count() > 1))
        {
            foreach (var handler in duplicateGroup)
            {
                ReportDuplicateNotificationHandlerOrder(notificationMetadataName, handler, duplicateGroup.Key, discoveryState);
            }
        }

        return [.. handlers
            .OrderByDescending(static handler => handler.HasExplicitOrder)
            .ThenBy(static handler => handler.Order)
            .ThenBy(static handler => handler.MetadataName, StringComparer.Ordinal)];
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
        IAssemblySymbol primaryAssembly,
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
            type,
            responseType,
            type.IsValueType,
            SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, primaryAssembly),
            type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? type.Locations.FirstOrDefault());

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
        var hasCompatibleHandleMethod = HasCompatibleRequestHandleMethod(type, requestType, responseType, discoverySymbols, primaryAssembly);
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, primaryAssembly, discoveryState);
        ReportInvalidHandlerSignature(type, GetTypeDisplayString(requestType), hasCompatibleHandleMethod, primaryAssembly, discoveryState);
        var descriptor = new HandlerDescriptor(
            GetMetadataName(type),
            GetNamespace(type),
            type.Name,
            GetTypeDisplayString(requestType),
            GetTypeDisplayString(responseType),
            GetHandleMethodName(type),
            type.DeclaredAccessibility,
            isAccessibleToGeneratedMediator,
            hasCompatibleHandleMethod,
            handlerKind.Value);

        if (isAccessibleToGeneratedMediator && hasCompatibleHandleMethod)
        {
            ReportDuplicateGeneratedRegistration(
                type,
                GetSelfRegistrationServiceType(descriptor.MetadataName),
                descriptor.MetadataName,
                primaryAssembly,
                discoveryState);
            ReportDuplicateGeneratedRegistration(
                type,
                GetHandlerServiceType(descriptor),
                descriptor.MetadataName,
                primaryAssembly,
                discoveryState);
        }

        return ($"{descriptor.RequestMetadataName}|{descriptor.ResponseMetadataName}", priority, descriptor);
    }

    private static IEnumerable<RawPipelineDescriptor> CreatePipelines(
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
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, primaryAssembly, discoveryState);

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

            yield return new RawPipelineDescriptor(
                GetMetadataName(type),
                type.TypeParameters.Length > 0 ? GetMetadataName(type.OriginalDefinition) : null,
                stage,
                order,
                type.TypeParameters.Length > 0 ? PipelineApplicability.OpenGeneric : PipelineApplicability.Closed,
                isAccessibleToGeneratedMediator,
                GetTypeDisplayString(typeArguments[0]),
                GetTypeDisplayString(typeArguments[1]),
                type,
                typeArguments[0],
                typeArguments[1],
                [.. type.TypeParameters]);

            if (isAccessibleToGeneratedMediator)
            {
                var implementationType = GetMetadataName(type);
                ReportDuplicateGeneratedRegistration(
                    type,
                    GetSelfRegistrationServiceType(implementationType),
                    implementationType,
                    primaryAssembly,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.IPipelineBehavior<{GetTypeDisplayString(typeArguments[0])}, {GetTypeDisplayString(typeArguments[1])}>",
                    implementationType,
                    primaryAssembly,
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
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, primaryAssembly, discoveryState);

        var notificationTypeArguments = type.AllInterfaces
            .Where(
                candidate => SymbolEqualityComparer.Default.Equals(
                    candidate.OriginalDefinition,
                    discoverySymbols.NotificationHandlerInterface))
            .Select(static candidate => candidate.TypeArguments);

        foreach (var typeArguments in notificationTypeArguments)
        {
            var notificationOrder = type.GetAttributes().SingleOrDefault(
                attribute => SymbolEqualityComparer.Default.Equals(
                    attribute.AttributeClass,
                    discoverySymbols.NotificationOrderAttribute));
            var hasExplicitOrder = notificationOrder is not null;
            var order = notificationOrder?.ConstructorArguments[0].Value is int orderValue ? orderValue : 0;

            yield return new NotificationHandlerDescriptor(
                GetMetadataName(type),
                GetNamespace(type),
                type.Name,
                GetTypeDisplayString(typeArguments[0]),
                GetHandleMethodName(type),
                type.DeclaredAccessibility,
                isAccessibleToGeneratedMediator,
                order,
                hasExplicitOrder,
                GetDiagnosticLocation(type, primaryAssembly));

            if (isAccessibleToGeneratedMediator)
            {
                var implementationType = GetMetadataName(type);
                ReportDuplicateGeneratedRegistration(
                    type,
                    GetSelfRegistrationServiceType(implementationType),
                    implementationType,
                    primaryAssembly,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.INotificationHandler<{GetTypeDisplayString(typeArguments[0])}>",
                    implementationType,
                    primaryAssembly,
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
        ReportInaccessibleRegistrationType(type, isAccessibleToGeneratedMediator, primaryAssembly, discoveryState);

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
                    primaryAssembly,
                    discoveryState);
                ReportDuplicateGeneratedRegistration(
                    type,
                    $"global::SharedKernel.Mediator.IStreamRequestHandler<{GetTypeDisplayString(typeArguments[0])}, {GetTypeDisplayString(typeArguments[1])}>",
                    implementationType,
                    primaryAssembly,
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
                    if (HasInternalAccessToPrimaryAssembly(current.ContainingAssembly, primaryAssembly))
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

    private static bool IsAccessibleToGeneratedMediator(ISymbol symbol, IAssemblySymbol primaryAssembly)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Internal or Accessibility.ProtectedOrInternal => HasInternalAccessToPrimaryAssembly(symbol.ContainingAssembly, primaryAssembly),
            _ => false,
        };
    }

    private static bool HasInternalAccessToPrimaryAssembly(IAssemblySymbol assembly, IAssemblySymbol primaryAssembly)
    {
        return SymbolEqualityComparer.Default.Equals(assembly, primaryAssembly)
               || assembly.GivesAccessTo(primaryAssembly);
    }

    private static void ReportInaccessibleRegistrationType(
        INamedTypeSymbol type,
        bool isAccessibleToGeneratedMediator,
        IAssemblySymbol primaryAssembly,
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

        var properties = ImmutableDictionary<string, string?>.Empty.Add(PrimaryAssemblyNamePropertyName, primaryAssembly.Identity.Name);
        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                InaccessibleRegistrationTypeDescriptor,
                GetDiagnosticLocation(location, SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, primaryAssembly)),
                properties,
                metadataName));
    }

    private static void ReportUnmarkedReferencedAssemblies(
        Compilation compilation,
        DiscoverySymbols discoverySymbols,
        INamedTypeSymbol? moduleAttribute,
        DiscoveryState discoveryState,
        CancellationToken cancellationToken)
    {
        if (moduleAttribute is null)
        {
            return;
        }

        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (HasModuleMarker(referencedAssembly, moduleAttribute)
                || !discoveryState.DiagnosedUnmarkedAssemblies.Add(referencedAssembly.Identity.Name)
                || !TryFindModuleCandidateLocation(referencedAssembly.GlobalNamespace, discoverySymbols, cancellationToken, out _))
            {
                continue;
            }

            discoveryState.Diagnostics.Add(
                Diagnostic.Create(
                    MissingModuleMarkerDescriptor,
                    Location.None,
                    referencedAssembly.Identity.Name));
            discoveryState.Diagnostics.Add(
                Diagnostic.Create(
                    UnprovenObjectDispatchCoverageDescriptor,
                    Location.None,
                    referencedAssembly.Identity.Name));
        }
    }

    private static bool TryFindModuleCandidateLocation(
        INamespaceSymbol @namespace,
        DiscoverySymbols discoverySymbols,
        CancellationToken cancellationToken,
        out Location? location)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var nestedNamespace in @namespace.GetNamespaceMembers())
        {
            if (TryFindModuleCandidateLocation(nestedNamespace, discoverySymbols, cancellationToken, out location))
            {
                return true;
            }
        }

        foreach (var type in @namespace.GetTypeMembers())
        {
            if (TryFindModuleCandidateLocation(type, discoverySymbols, cancellationToken, out location))
            {
                return true;
            }
        }

        location = null;
        return false;
    }

    private static bool TryFindModuleCandidateLocation(
        INamedTypeSymbol type,
        DiscoverySymbols discoverySymbols,
        CancellationToken cancellationToken,
        out Location? location)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ContainsModuleCandidate(type, discoverySymbols))
        {
            location = type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? type.Locations.FirstOrDefault();
            return location is not null;
        }

        foreach (var nestedType in type.GetTypeMembers())
        {
            if (TryFindModuleCandidateLocation(nestedType, discoverySymbols, cancellationToken, out location))
            {
                return true;
            }
        }

        location = null;
        return false;
    }

    private static bool ContainsModuleCandidate(INamedTypeSymbol type, DiscoverySymbols discoverySymbols)
    {
        if (!IsDiscoverableType(type))
        {
            return false;
        }

        if (FindBestRequestInterface(type, discoverySymbols) is not null
            || ImplementsInterface(type, discoverySymbols.NotificationInterface)
            || ImplementsInterface(type, discoverySymbols.StreamRequestInterface))
        {
            return true;
        }

        return type.AllInterfaces.Any(
            candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.HandlerInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.CommandHandlerInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.CommandHandlerOfResponseInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.QueryHandlerInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.PipelineInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.NotificationHandlerInterface)
                         || SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, discoverySymbols.StreamHandlerInterface));
    }

    private static void ReportDuplicateGeneratedRegistration(
        INamedTypeSymbol type,
        string serviceType,
        string implementationType,
        IAssemblySymbol primaryAssembly,
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

        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                DuplicateGeneratedRegistrationDescriptor,
                GetDiagnosticLocation(type, primaryAssembly),
                serviceType,
                implementationType));
    }

    private static void ReportRequestHandlerDiagnostics(
        IEnumerable<RequestDescriptor> requests,
        DiscoveryState discoveryState)
    {
        foreach (var request in requests)
        {
            var compatibleAccessibleHandlerCount = request.Handlers.Count(
                static handler => handler.IsAccessibleToGeneratedMediator && handler.HasCompatibleHandleMethod);

            if (compatibleAccessibleHandlerCount == 0)
            {
                ReportRequestDiagnostic(MissingHandlerDescriptor, request, compatibleAccessibleHandlerCount, discoveryState);
                continue;
            }

            if (compatibleAccessibleHandlerCount > 1)
            {
                ReportRequestDiagnostic(MultipleHandlersDescriptor, request, compatibleAccessibleHandlerCount, discoveryState);
            }
        }
    }

    private static void ReportRequestDiagnostic(
        DiagnosticDescriptor descriptor,
        RequestDescriptor request,
        int handlerCount,
        DiscoveryState discoveryState)
    {
        if (!discoveryState.RequestContracts.TryGetValue(request.MetadataName, out var requestContract))
        {
            return;
        }

        var location = GetDiagnosticLocation(requestContract.Location, requestContract.IsInPrimaryAssembly);

        var diagnostic = descriptor.Id == MediatorDiagnosticIds.MissingHandler
            ? Diagnostic.Create(descriptor, location, request.MetadataName)
            : Diagnostic.Create(descriptor, location, request.MetadataName, handlerCount);
        discoveryState.Diagnostics.Add(diagnostic);
    }

    private static void ReportInvalidHandlerSignature(
        INamedTypeSymbol type,
        string requestMetadataName,
        bool hasCompatibleHandleMethod,
        IAssemblySymbol primaryAssembly,
        DiscoveryState discoveryState)
    {
        if (hasCompatibleHandleMethod)
        {
            return;
        }

        var key = $"{GetMetadataName(type)}|{requestMetadataName}";
        if (!discoveryState.DiagnosedInvalidHandlerKeys.Add(key))
        {
            return;
        }

        discoveryState.Diagnostics.Add(
            Diagnostic.Create(
                InvalidHandlerSignatureDescriptor,
                GetDiagnosticLocation(type, primaryAssembly),
                GetMetadataName(type),
                requestMetadataName));
    }

    private static Location GetDiagnosticLocation(INamedTypeSymbol type, IAssemblySymbol primaryAssembly)
    {
        var location = type.Locations.FirstOrDefault(static candidate => candidate.IsInSource) ?? type.Locations.FirstOrDefault();
        return GetDiagnosticLocation(location, SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, primaryAssembly));
    }

    private static Location GetDiagnosticLocation(Location? location, bool isInPrimaryAssembly)
    {
        if (!isInPrimaryAssembly || location is null)
        {
            return Location.None;
        }

        return location;
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

    private static bool HasCompatibleRequestHandleMethod(
        INamedTypeSymbol type,
        ITypeSymbol requestType,
        ITypeSymbol responseType,
        DiscoverySymbols discoverySymbols,
        IAssemblySymbol primaryAssembly)
    {
        return type.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .Any(
                method =>
                    !method.IsStatic
                    && method.MethodKind == MethodKind.Ordinary
                    && method.Parameters.Length == 2
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, requestType)
                    && SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, discoverySymbols.CancellationTokenType)
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, ConstructValueTaskResponse(discoverySymbols, responseType))
                    && IsAccessibleToGeneratedMediator(method, primaryAssembly));
    }

    private static INamedTypeSymbol ConstructValueTaskResponse(DiscoverySymbols discoverySymbols, ITypeSymbol responseType)
    {
        return discoverySymbols.ValueTaskOfT.Construct(responseType);
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
