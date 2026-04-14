using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class AddressPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public AddressPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_When_State_Already_Has_Address_Preloads_Existing_Values()
    {
        // Arrange
        _state.SetAddress(new AddressFormModel
        {
            Street = "Rua das Flores, 123",
            Complement = "Apt 45",
            Neighborhood = "Centro",
            PostalCode = "01000-000",
            City = "São Paulo",
            State = "SP",
            Country = "Brazil",
        });

        // Act
        var cut = Render<Address>();

        // Assert
        Assert.Equal("Rua das Flores, 123", cut.Find("#street").GetAttribute("value"));
        Assert.Equal("Apt 45", cut.Find("#complement").GetAttribute("value"));
        Assert.Equal("Centro", cut.Find("#neighborhood").GetAttribute("value"));
        Assert.Equal("01000-000", cut.Find("#postalCode").GetAttribute("value"));
        Assert.Equal("São Paulo", cut.Find("#city").GetAttribute("value"));
        Assert.Equal("SP", cut.Find("#state").GetAttribute("value"));
        Assert.Equal("Brazil", cut.Find("#country").GetAttribute("value"));
        Assert.Equal(4, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Physical()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Address>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#street").Change("Rua das Flores, 123"));
        await cut.InvokeAsync(() => cut.Find("#complement").Change("Apt 45"));
        await cut.InvokeAsync(() => cut.Find("#neighborhood").Change("Centro"));
        await cut.InvokeAsync(() => cut.Find("#postalCode").Change("01000-000"));
        await cut.InvokeAsync(() => cut.Find("#city").Change("São Paulo"));
        await cut.InvokeAsync(() => cut.Find("#state").Change("SP"));
        await cut.InvokeAsync(() => cut.Find("#country").Change("Brazil"));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/physical", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.Address);
        Assert.Equal("Rua das Flores, 123", _state.Address!.Street);
        Assert.Equal("Apt 45", _state.Address.Complement);
        Assert.Equal("Centro", _state.Address.Neighborhood);
        Assert.Equal("01000-000", _state.Address.PostalCode);
        Assert.Equal("São Paulo", _state.Address.City);
        Assert.Equal("SP", _state.Address.State);
        Assert.Equal("Brazil", _state.Address.Country);
        Assert.Equal(5, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_Button_Navigates_To_Contact_And_Updates_Current_Step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Address>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/contact", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(3, _state.CurrentStep);
    }
}
