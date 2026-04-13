using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class EmergencyContactPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public EmergencyContactPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_When_State_Already_Has_Emergency_Contact_Preloads_Existing_Values()
    {
        // Arrange
        _state.SetEmergencyContact(new EmergencyContactFormModel
        {
            Name = "Maria Silva",
            Mobile = "+55 11 98888-8888",
        });

        // Act
        var cut = Render<EmergencyContact>();

        // Assert
        Assert.Equal("Maria Silva", cut.Find("#name").GetAttribute("value"));
        Assert.Equal("+55 11 98888-8888", cut.Find("#mobile").GetAttribute("value"));
        Assert.Equal(7, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Medical()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<EmergencyContact>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#name").Change("Maria Silva"));
        await cut.InvokeAsync(() => cut.Find("#mobile").Change("+55 11 98888-8888"));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/medical", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.EmergencyContact);
        Assert.Equal("Maria Silva", _state.EmergencyContact!.Name);
        Assert.Equal("+55 11 98888-8888", _state.EmergencyContact.Mobile);
        Assert.Equal(8, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_Button_Navigates_To_Accommodation_And_Updates_Current_Step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<EmergencyContact>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/accommodation", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(6, _state.CurrentStep);
    }
}
