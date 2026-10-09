using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class ContactPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public ContactPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_when_state_already_has_contact_info_preloads_existing_values()
    {
        // Arrange
        _state.SetContactInfo(new ContactInfoFormModel
        {
            Email = "ana.silva@example.com",
            Mobile = "+55 11 98765-4321",
            Instagram = "@ana.silva",
            Facebook = "facebook.com/ana.silva",
        });

        // Act
        var cut = Render<Contact>();

        // Assert
        (cut.Find("#email").GetAttribute("value")).ShouldBe("ana.silva@example.com");
        (cut.Find("#mobile").GetAttribute("value")).ShouldBe("+55 11 98765-4321");
        (cut.Find("#instagram").GetAttribute("value")).ShouldBe("@ana.silva");
        (cut.Find("#facebook").GetAttribute("value")).ShouldBe("facebook.com/ana.silva");
        (_state.CurrentStep).ShouldBe(3);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_address()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Contact>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#email").Change("ana.silva@example.com"));
        await cut.InvokeAsync(() => cut.Find("#mobile").Change("+55 11 98765-4321"));
        await cut.InvokeAsync(() => cut.Find("#instagram").Change("@ana.silva"));
        await cut.InvokeAsync(() => cut.Find("#facebook").Change("facebook.com/ana.silva"));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/address", StringComparison.Ordinal));
        (_state.ContactInfo).ShouldNotBeNull();
        (_state.ContactInfo.Email).ShouldBe("ana.silva@example.com");
        (_state.ContactInfo.Mobile).ShouldBe("+55 11 98765-4321");
        (_state.ContactInfo.Instagram).ShouldBe("@ana.silva");
        (_state.ContactInfo.Facebook).ShouldBe("facebook.com/ana.silva");
        (_state.CurrentStep).ShouldBe(4);
    }

    [Fact]
    public async Task Back_button_navigates_to_identification_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Contact>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/identification", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(2);
    }
}
