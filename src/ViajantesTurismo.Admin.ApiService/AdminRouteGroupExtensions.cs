namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Provides the Admin API's canonical route-group mappings and OpenAPI document names.
/// </summary>
internal static class AdminRouteGroupExtensions
{
    private const string ToursRoutePrefix = "tours";
    private const string ToursGroupName = "Tours";
    private const string CustomersRoutePrefix = "customers";
    private const string CustomersGroupName = "Customers";
    private const string BookingsRoutePrefix = "bookings";
    private const string BookingsGroupName = "Bookings";

    /// <summary>
    /// Gets the OpenAPI document names used by the Admin API.
    /// </summary>
    public static IReadOnlyCollection<string> OpenApiDocumentNames { get; } =
    [
        ToursRoutePrefix,
        CustomersRoutePrefix,
        BookingsRoutePrefix
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

    private static RouteGroupBuilder MapRouteGroup(this WebApplication app, string routePrefix, string groupName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return app.MapGroup($"/{routePrefix}")
            .WithGroupName(groupName)
            .WithTags(groupName);
    }
}
