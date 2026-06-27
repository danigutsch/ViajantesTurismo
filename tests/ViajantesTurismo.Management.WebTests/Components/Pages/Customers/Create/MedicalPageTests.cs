using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class MedicalPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public MedicalPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_when_state_already_has_medical_info_preloads_existing_values()
    {
        // Arrange
        _state.SetMedicalInfo(new MedicalInfoFormModel
        {
            Allergies = "Peanuts",
            AdditionalInfo = "Carries an epinephrine injector.",
        });

        // Act
        var cut = Render<Medical>();

        // Assert
        var allergiesValue = cut.Find("#allergies").GetAttribute("value") ?? cut.Find("#allergies").TextContent.Trim();
        var additionalInfoValue = cut.Find("#additionalInfo").GetAttribute("value") ?? cut.Find("#additionalInfo").TextContent.Trim();

        Assert.Equal("Peanuts", allergiesValue);
        Assert.Equal("Carries an epinephrine injector.", additionalInfoValue);
        Assert.Equal(8, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_review()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Medical>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#allergies").Change("Peanuts"));
        await cut.InvokeAsync(() => cut.Find("#additionalInfo").Change("Carries an epinephrine injector."));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/review", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.MedicalInfo);
        Assert.Equal("Peanuts", _state.MedicalInfo!.Allergies);
        Assert.Equal("Carries an epinephrine injector.", _state.MedicalInfo.AdditionalInfo);
        Assert.Equal(8, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_button_navigates_to_emergency_contact_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Medical>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/emergency-contact", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(7, _state.CurrentStep);
    }
}
