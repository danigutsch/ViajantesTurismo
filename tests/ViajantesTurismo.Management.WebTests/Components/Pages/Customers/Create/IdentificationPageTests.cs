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
    public async Task OnInitialized_when_state_already_has_identificationInfo_preloads_existing_values()
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
        Assert.Equal("123456789", cut.Find("#nationalId").GetAttribute("value"));
        Assert.Contains("Brazil", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(2, _state.CurrentStep);
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
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/contact", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.IdentificationInfo);
        Assert.Equal("123456789", _state.IdentificationInfo!.NationalId);
        Assert.Equal("Brazil", _state.IdentificationInfo.IdNationality);
        Assert.Equal(3, _state.CurrentStep);
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
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/personal-info", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(1, _state.CurrentStep);
    }
}
