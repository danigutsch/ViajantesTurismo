using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SharedKernel.AspNetCore.Authentication;
using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class DocumentMaintenanceControlAuthorizationTests
{
    [Theory]
    [InlineData("pause")]
    [InlineData("resume")]
    [InlineData("trigger")]
    [InlineData("retry")]
    public async Task A_read_only_operations_role_cannot_control_the_schedule(string control)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = AdminApiMutationRequestFactory.Create("POST", $"/api/v1/operations/document-maintenance/drafts/{control}");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_raw_permission_claim_cannot_control_the_schedule()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create(services =>
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                var onTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    await onTokenValidated(context);
                    var identity = (ClaimsIdentity)context.Principal!.Identity!;
                    identity.AddClaim(new Claim(ApiAuthenticationDefaults.PermissionClaimType, "operations.schedule.control"));
                };
            }));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Admin");
        using var request = AdminApiMutationRequestFactory.Create("POST", "/api/v1/operations/document-maintenance/drafts/trigger");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_dedicated_role_can_reach_schedule_control_validation()
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "OperationsScheduleController");
        using var request = AdminApiMutationRequestFactory.Create("POST", "/api/v1/operations/document-maintenance/drafts/trigger");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

}
