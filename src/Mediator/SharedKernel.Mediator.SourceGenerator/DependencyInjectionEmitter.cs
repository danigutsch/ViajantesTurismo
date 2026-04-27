using System.Text;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Emits generated Microsoft.Extensions.DependencyInjection registrations from discovery data.
/// </summary>
internal static class DependencyInjectionEmitter
{
    private const string AddTransientMethodName = "AddTransient";

    public static string Emit(DiscoveryModel model)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine();
        builder.AppendLine("namespace SharedKernel.Mediator;");
        builder.AppendLine();
        builder.AppendLine("public static partial class SharedKernelMediatorServiceCollectionExtensions");
        builder.AppendLine("{");
        builder.AppendLine("    public static IServiceCollection AddSharedKernelMediator(this IServiceCollection services)");
        builder.AppendLine("    {");
        builder.AppendLine("        global::System.ArgumentNullException.ThrowIfNull(services);");

        var emittedRequestRegistrations = EmitRequestRegistrations(builder, model.Requests);
        var emittedNotificationRegistrations = EmitNotificationRegistrations(builder, model.Notifications, emittedRequestRegistrations);
        var emittedStreamRegistrations = EmitStreamRegistrations(
            builder,
            model.StreamRequests,
            emittedRequestRegistrations || emittedNotificationRegistrations);

        if (emittedRequestRegistrations || emittedNotificationRegistrations || emittedStreamRegistrations)
        {
            builder.AppendLine();
        }

        builder.AppendLine("        return services;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static bool EmitRequestRegistrations(StringBuilder builder, IEnumerable<RequestDescriptor> requests)
    {
        var emittedAny = false;

        foreach (var request in requests)
        {
            foreach (var handler in request.Handlers)
            {
                EmitRegistration(
                    builder,
                    AddTransientMethodName,
                    handler.MetadataName,
                    GetHandlerServiceType(request, handler));
                emittedAny = true;
            }

            foreach (var pipeline in request.Pipelines)
            {
                EmitRegistration(
                    builder,
                    AddTransientMethodName,
                    pipeline.MetadataName,
                    $"global::SharedKernel.Mediator.IPipelineBehavior<{request.MetadataName}, {request.Response.MetadataName}>");
                emittedAny = true;
            }
        }

        return emittedAny;
    }

    private static bool EmitNotificationRegistrations(
        StringBuilder builder,
        IEnumerable<NotificationDescriptor> notifications,
        bool hasPriorRegistrations)
    {
        var emittedAny = false;

        foreach (var notification in notifications)
        {
            if (!emittedAny && hasPriorRegistrations)
            {
                builder.AppendLine();
            }

            foreach (var handler in notification.Handlers)
            {
                EmitRegistration(
                    builder,
                    AddTransientMethodName,
                    handler.MetadataName,
                    $"global::SharedKernel.Mediator.INotificationHandler<{notification.MetadataName}>");
                emittedAny = true;
            }
        }

        return emittedAny;
    }

    private static bool EmitStreamRegistrations(
        StringBuilder builder,
        IEnumerable<StreamRequestDescriptor> streamRequests,
        bool hasPriorRegistrations)
    {
        var emittedAny = false;

        foreach (var streamRequest in streamRequests)
        {
            if (!emittedAny && hasPriorRegistrations)
            {
                builder.AppendLine();
            }

            foreach (var handler in streamRequest.Handlers)
            {
                EmitRegistration(
                    builder,
                    AddTransientMethodName,
                    handler.MetadataName,
                    $"global::SharedKernel.Mediator.IStreamRequestHandler<{streamRequest.MetadataName}, {streamRequest.ItemResponse.MetadataName}>");
                emittedAny = true;
            }
        }

        return emittedAny;
    }

    private static void EmitRegistration(StringBuilder builder, string methodName, string implementationType, string serviceType)
    {
        builder.Append("        services.").Append(methodName).Append('<').Append(implementationType).AppendLine(">();");
        builder.Append("        services.").Append(methodName).Append('<').Append(serviceType).Append(", ").Append(implementationType).AppendLine(">();");
    }

    private static string GetHandlerServiceType(RequestDescriptor request, HandlerDescriptor handler)
    {
        return handler.Kind switch
        {
            HandlerKind.Request => $"global::SharedKernel.Mediator.IRequestHandler<{request.MetadataName}, {request.Response.MetadataName}>",
            HandlerKind.Command => $"global::SharedKernel.Mediator.ICommandHandler<{request.MetadataName}>",
            HandlerKind.CommandWithResponse => $"global::SharedKernel.Mediator.ICommandHandler<{request.MetadataName}, {request.Response.MetadataName}>",
            HandlerKind.Query => $"global::SharedKernel.Mediator.IQueryHandler<{request.MetadataName}, {request.Response.MetadataName}>",
            _ => throw new ArgumentOutOfRangeException(nameof(handler))
        };
    }
}
