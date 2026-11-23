using System.Reflection;
using ViajantesTurismo.Admin.Infrastructure.Documents;

namespace ViajantesTurismo.Admin.Api.Documents;

/// <summary>Measures the outcome of authorized document workflow requests.</summary>
internal static class DocumentEndpointTelemetry
{
    private static readonly object CorrelationKey = new();

    /// <summary>Measures the annotated workflows in the group before handing off their responses.</summary>
    public static RouteGroupBuilder ObserveDocumentOperations(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var telemetry = ((IEndpointRouteBuilder)group).ServiceProvider.GetService<DocumentOperationTelemetry>();
        if (telemetry is null)
        {
            return group;
        }

        return group.AddEndpointFilterFactory((factory, next) =>
        {
            if (factory.MethodInfo.GetCustomAttribute<ObserveDocumentOperationAttribute>() is not { Operation: var operation })
            {
                return next;
            }

            return context =>
            {
                var http = context.HttpContext;
                return telemetry.Record(operation, GetCorrelationId(http), () => next(context), Classify, http.RequestAborted);
            };
        });
    }

    /// <summary>Gets the server-created identifier shared by request telemetry and audit evidence.</summary>
    public static Guid GetCorrelationId(HttpContext context)
    {
        if (context.Items[CorrelationKey] is Guid existing)
        {
            return existing;
        }

        var correlationId = Guid.CreateVersion7();
        context.Items[CorrelationKey] = correlationId;
        return correlationId;
    }

    private static DocumentOperationOutcome Classify(object? response)
    {
        while (response is INestedHttpResult nested)
        {
            response = nested.Result;
        }

        return (response as IStatusCodeHttpResult)?.StatusCode switch
        {
            >= StatusCodes.Status500InternalServerError => DocumentOperationOutcome.Failed,
            >= StatusCodes.Status400BadRequest => DocumentOperationOutcome.Rejected,
            _ => DocumentOperationOutcome.Succeeded,
        };
    }
}
