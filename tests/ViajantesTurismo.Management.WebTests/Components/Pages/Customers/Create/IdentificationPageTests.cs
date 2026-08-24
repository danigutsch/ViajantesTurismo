using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;
using ViajantesTurismo.Management.WebTests.Infrastructure;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class IdentificationPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public IdentificationPageTests()
    {
        Services.AddSingleton(_state);
        Services.AddSingleton<ICountryService>(new FakeCountryService());
    }

    [Fact]
    public async Task OnInitialized_when_state_already_has_identificationinfo_preloads_existing_values()
    {
        // Arrange
        _state.SetIdentificationInfo(new IdentificationInfoFormModel
        {
            NationalId = "123456789",
            IdNationality = "Brazil",
        });

        // Act
        var cut = Render<Identification>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        (cut.Find("#nationalId").GetAttribute("value")).ShouldBe("123456789");
        (cut.Markup).ShouldContain("Brazil", StringComparison.Ordinal);
        (_state.CurrentStep).ShouldBe(2);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_contact()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Identification>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.InvokeAsync(() => cut.Find("#nationalId").Change("123456789"));
        await cut.InvokeAsync(() => cut.Find("button.form-select").Click());
        await cut.InvokeAsync(() => cut.FindAll(".country-dropdown-item").First(item => item.TextContent.Contains("Brazil", StringComparison.Ordinal)).Click());
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/contact", StringComparison.Ordinal));
        (_state.IdentificationInfo).ShouldNotBeNull();
        (_state.IdentificationInfo.NationalId).ShouldBe("123456789");
        (_state.IdentificationInfo.IdNationality).ShouldBe("Brazil");
        (_state.CurrentStep).ShouldBe(3);
    }

    [Fact]
    public async Task Back_button_navigates_to_personal_info_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Identification>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/personal-info", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(1);
    }
}
