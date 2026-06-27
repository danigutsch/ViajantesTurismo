using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class EmergencyContactPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public EmergencyContactPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_when_state_already_has_emergency_contact_preloads_existing_values()
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
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_medical()
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
    public async Task Back_button_navigates_to_accommodation_and_updates_current_step()
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
