using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class DocumentMaintenanceAuthorizationTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    public async Task Document_maintenance_history_requires_the_admin_role(string? role, HttpStatusCode expected)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/operations/document-maintenance", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }
}
