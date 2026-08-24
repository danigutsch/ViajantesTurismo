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

        (allergiesValue).ShouldBe("Peanuts");
        (additionalInfoValue).ShouldBe("Carries an epinephrine injector.");
        (_state.CurrentStep).ShouldBe(8);
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
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/review", StringComparison.Ordinal));
        (_state.MedicalInfo).ShouldNotBeNull();
        (_state.MedicalInfo.Allergies).ShouldBe("Peanuts");
        (_state.MedicalInfo.AdditionalInfo).ShouldBe("Carries an epinephrine injector.");
        (_state.CurrentStep).ShouldBe(8);
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
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/emergency-contact", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(7);
    }
}
