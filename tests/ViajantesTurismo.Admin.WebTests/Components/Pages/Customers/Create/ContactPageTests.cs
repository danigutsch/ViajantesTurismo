using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class ContactPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public ContactPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_When_State_Already_Has_Contact_Info_Preloads_Existing_Values()
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
        Assert.Equal("ana.silva@example.com", cut.Find("#email").GetAttribute("value"));
        Assert.Equal("+55 11 98765-4321", cut.Find("#mobile").GetAttribute("value"));
        Assert.Equal("@ana.silva", cut.Find("#instagram").GetAttribute("value"));
        Assert.Equal("facebook.com/ana.silva", cut.Find("#facebook").GetAttribute("value"));
        Assert.Equal(3, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Address()
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
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/address", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.ContactInfo);
        Assert.Equal("ana.silva@example.com", _state.ContactInfo!.Email);
        Assert.Equal("+55 11 98765-4321", _state.ContactInfo.Mobile);
        Assert.Equal("@ana.silva", _state.ContactInfo.Instagram);
        Assert.Equal("facebook.com/ana.silva", _state.ContactInfo.Facebook);
        Assert.Equal(4, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_Button_Navigates_To_Identification_And_Updates_Current_Step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Contact>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/identification", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(2, _state.CurrentStep);
    }
}
