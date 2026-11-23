using PandaBear.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraitValues.TestServerHost)]
public sealed class AdminApiMaintenanceTests
{
    [Fact]
    public async Task Maintenance_replaces_an_ordinary_API_response_with_an_uncacheable_problem()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(configureConfiguration: options =>
            options[HttpMaintenanceConfiguration.EnabledKey] = "true");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/robots.txt", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/problem+json");
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    [InlineData("Admin", HttpStatusCode.ServiceUnavailable)]
    public async Task Maintenance_preserves_authorization_for_protected_mutations(string? role, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(configureConfiguration: options =>
            options[HttpMaintenanceConfiguration.EnabledKey] = "true");
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.DeleteAsync(new Uri($"/api/v1/tours/{Guid.CreateVersion7()}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
    }
}
