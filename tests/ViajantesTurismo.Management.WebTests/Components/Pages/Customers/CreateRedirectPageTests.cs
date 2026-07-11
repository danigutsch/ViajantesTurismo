using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers;

public sealed class CreateRedirectPageTests : BunitContext
{
    [Fact]
    public void Redirects_to_personal_info_page_on_first_render()
    {
        // Arrange
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = Render<CreateRedirect>();
        cut.WaitForState(() => navManager.Uri.EndsWith("/customers/create/personal-info", StringComparison.Ordinal));

        // Assert
        (navManager.Uri).ShouldEndWith("/customers/create/personal-info", StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_without_content()
    {
        // Act
        var cut = Render<CreateRedirect>();

        // Assert
        (cut.Markup.Trim()).ShouldBeEmpty();
    }
}
