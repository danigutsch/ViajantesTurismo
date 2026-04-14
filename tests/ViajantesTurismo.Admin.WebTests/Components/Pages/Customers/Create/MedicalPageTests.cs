using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class MedicalPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public MedicalPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_When_State_Already_Has_Medical_Info_Preloads_Existing_Values()
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
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Review()
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
    public async Task Back_Button_Navigates_To_Emergency_Contact_And_Updates_Current_Step()
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
