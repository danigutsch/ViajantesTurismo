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
    [InlineData("Reader", "pause", HttpStatusCode.Forbidden)]
    [InlineData("Reader", "retry", HttpStatusCode.Forbidden)]
    [InlineData("Operator", "resume", HttpStatusCode.Forbidden)]
    [InlineData("Operator", "trigger", HttpStatusCode.Forbidden)]
    [InlineData(null, "trigger", HttpStatusCode.Unauthorized)]
    public async Task An_unauthorized_session_cannot_control_the_schedule(string? role, string control, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        }
        using var request = AdminApiMutationRequestFactory.Create("POST", $"/api/v1/operations/document-maintenance/drafts/{control}");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expected);
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
                    var identity = context.Principal?.Identity as ClaimsIdentity
                        ?? throw new InvalidOperationException("The test token requires a claims identity.");
                    identity.AddClaim(new Claim(ApiAuthenticationDefaults.PermissionClaimType, "operations.schedule.control"));
                };
            }));
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, "Reader");
        using var request = AdminApiMutationRequestFactory.Create("POST", "/api/v1/operations/document-maintenance/drafts/trigger");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("OperationsScheduleController")]
    public async Task An_authorized_role_still_requires_valid_schedule_control_input(string role)
    {
        // Arrange
        await using var factory = AdminApiTestHost.Create();
        using var client = factory.CreateClient();
        AdminApiTestHost.ConfigureAuthenticatedClient(client, role);
        using var request = AdminApiMutationRequestFactory.Create("POST", "/api/v1/operations/document-maintenance/drafts/trigger");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

}
