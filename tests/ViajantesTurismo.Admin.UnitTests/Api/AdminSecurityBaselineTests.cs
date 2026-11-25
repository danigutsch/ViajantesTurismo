using Microsoft.Extensions.Configuration;
using ViajantesTurismo.Admin.Api;

namespace ViajantesTurismo.Admin.UnitTests.Api;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class AdminSecurityBaselineTests
{
    [Fact]
    public void Configures_admin_CORS_policy_with_allowed_origins()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Cors:AllowedOrigins:0"] = "https://admin.example.com"
            })
            .Build();

        // Act
        var corsOptions = AdminSecurityBaselineTestServices.GetCorsOptions(configuration);

        // Assert
        var policy = corsOptions.GetPolicy(AdminSecurityBaseline.CorsPolicyName).ShouldNotBeNull();
        policy.Origins.ShouldContain("https://admin.example.com");
    }
}
