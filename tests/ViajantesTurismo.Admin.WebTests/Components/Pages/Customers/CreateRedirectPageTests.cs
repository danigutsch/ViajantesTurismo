using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers;

public sealed class CreateRedirectPageTests : BunitContext
{
    [Fact]
    public void Redirects_To_Personal_Info_Page_On_First_Render()
    {
        // Arrange
        var navManager = Services.GetRequiredService<NavigationManager>();

        // Act
        var cut = Render<CreateRedirect>();
        cut.WaitForState(() => navManager.Uri.EndsWith("/customers/create/personal-info", StringComparison.Ordinal));

        // Assert
        Assert.EndsWith("/customers/create/personal-info", navManager.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Renders_Without_Content()
    {
        // Act
        var cut = Render<CreateRedirect>();

        // Assert
        Assert.Empty(cut.Markup.Trim());
    }
}
