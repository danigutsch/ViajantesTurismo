using SharedKernel.ApiVersioning;
using SharedKernel.ApiVersioning.AspNetCore;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Provides the Admin API's canonical route-group mappings and OpenAPI document names.
/// </summary>
internal static class AdminRouteGroupExtensions
{
    private static readonly ApiVersionDefinition CurrentApiVersion = new(new ApiVersion(1));
    private const string ToursRoutePrefix = "tours";
    private const string ToursGroupName = "Tours";
    private const string CustomersRoutePrefix = "customers";
    private const string CustomersGroupName = "Customers";
    private const string BookingsRoutePrefix = "bookings";
    private const string BookingsGroupName = "Bookings";
    private const string DocumentsRoutePrefix = "documents";
    private const string DocumentsGroupName = "Documents";

    /// <summary>
    /// Gets the OpenAPI document names used by the Admin API.
    /// </summary>
    public static IReadOnlyCollection<string> OpenApiDocumentNames { get; } =
    [
        ToursRoutePrefix,
        CustomersRoutePrefix,
        BookingsRoutePrefix,
        DocumentsRoutePrefix,
        CurrentApiVersion.OpenApiDocumentName
    ];

    /// <summary>
    /// Gets the Admin API's active HTTP contract versions.
    /// </summary>
    public static IReadOnlyCollection<ApiVersionDefinition> ApiVersions { get; } =
    [
        CurrentApiVersion
    ];

    /// <summary>
    /// Maps the tours route group with the correct OpenAPI metadata.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapToursGroup(this WebApplication app)
    {
        return app.MapRouteGroup(ToursRoutePrefix, ToursGroupName);
    }

    /// <summary>
    /// Maps the customers route group with the correct OpenAPI metadata.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapCustomersGroup(this WebApplication app)
    {
        return app.MapRouteGroup(CustomersRoutePrefix, CustomersGroupName);
    }

    /// <summary>
    /// Maps the customer import route group with the correct OpenAPI metadata.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapCustomerImportsGroup(this WebApplication app)
    {
        return app.MapRouteGroup($"{CustomersRoutePrefix}/import", CustomersGroupName);
    }

    /// <summary>
    /// Maps the bookings route group with the correct OpenAPI metadata.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapBookingsGroup(this WebApplication app)
    {
        return app.MapRouteGroup(BookingsRoutePrefix, BookingsGroupName);
    }

    /// <summary>
    /// Maps the generated-documents route group with the correct OpenAPI metadata.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapDocumentsGroup(this WebApplication app)
    {
        return app.MapRouteGroup(DocumentsRoutePrefix, DocumentsGroupName);
    }

    /// <summary>
    /// Maps the API error documentation route group with the current API version prefix.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured route group builder.</returns>
    public static RouteGroupBuilder MapErrorDocumentationGroup(this WebApplication app)
    {
        return app.MapRouteGroup("docs/errors", "Errors");
    }

    private static RouteGroupBuilder MapRouteGroup(this WebApplication app, string routePrefix, string groupName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return app.MapApiVersionGroup(CurrentApiVersion)
            .MapGroup($"/{routePrefix}")
            .WithGroupName(groupName)
            .WithTags(groupName);
    }
}
