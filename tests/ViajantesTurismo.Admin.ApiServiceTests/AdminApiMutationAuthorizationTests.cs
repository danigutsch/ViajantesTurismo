namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, SharedKernel.Testing.TestTraitValues.TestServerHost)]
public sealed class AdminApiMutationAuthorizationTests
{
    [Fact]
    public async Task Admin_mutation_endpoints_reject_anonymous_requests()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        (string Method, string Path)[] mutationRequests =
        [
            ("POST", "/api/v1/tours/"),
            ("PUT", "/api/v1/tours/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/customers/"),
            ("PUT", "/api/v1/customers/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/customers/import/"),
            ("POST", "/api/v1/customers/import/commit"),
            ("POST", "/api/v1/bookings/"),
            ("PUT", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/discount"),
            ("PUT", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/details"),
            ("DELETE", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/cancel"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/confirm"),
            ("PATCH", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/notes"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/complete"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/payments")
        ];

        // Act
        foreach (var (method, path) in mutationRequests)
        {
            using var request = AdminApiMutationRequestFactory.Create(method, path);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Admin_mutation_endpoints_reject_authenticated_callers_without_permissions()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Guest");
        (string Method, string Path)[] mutationRequests =
        [
            ("POST", "/api/v1/tours/"),
            ("PUT", "/api/v1/tours/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/customers/"),
            ("PUT", "/api/v1/customers/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/customers/import/"),
            ("POST", "/api/v1/customers/import/commit"),
            ("POST", "/api/v1/bookings/"),
            ("PUT", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/discount"),
            ("PUT", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/details"),
            ("DELETE", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/cancel"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/confirm"),
            ("PATCH", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/notes"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/complete"),
            ("POST", "/api/v1/bookings/1d02ec44-41b5-4d3a-878b-89f53261a803/payments")
        ];

        // Act
        foreach (var (method, path) in mutationRequests)
        {
            using var request = AdminApiMutationRequestFactory.Create(method, path);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }
    }
}
