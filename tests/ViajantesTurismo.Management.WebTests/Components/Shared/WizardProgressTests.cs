using Microsoft.AspNetCore.Components;
using PandaBear.Testing;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Shared;

[TestScope(TestScope.Component)]
public sealed class WizardProgressTests : BunitContext
{
    public WizardProgressTests()
    {
        Services.AddScoped<CustomerCreationState>();
    }

    [Fact]
    public void Style_block_carries_the_cascaded_response_nonce()
    {
        // Arrange & Act
        var cut = Render<CascadingValue<string>>(parameters => parameters
            .Add(cascade => cascade.Name, ManagementWebSecurityHeaders.NonceCascadeName)
            .Add(cascade => cascade.Value, "bm9uY2U=")
            .Add(cascade => cascade.IsFixed, true)
            .AddChildContent<WizardProgress>());

        // Assert
        cut.Find("style").GetAttribute("nonce").ShouldBe("bm9uY2U=");
    }
}
