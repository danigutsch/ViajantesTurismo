using Microsoft.Extensions.Configuration;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Api;

namespace ViajantesTurismo.Admin.UnitTests.Api;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
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
