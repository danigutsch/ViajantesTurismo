using SharedKernel.OpenApi;

namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminOpenApiDocuments
{
    private static readonly OpenApiBoundaryDocument[] Documents =
    [
        new("tours", "tours"),
        new("customers", "customers"),
        new("bookings", "bookings")
    ];

    public static void AddAdminOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBoundaryOpenApiDocuments(Documents);
    }
}
