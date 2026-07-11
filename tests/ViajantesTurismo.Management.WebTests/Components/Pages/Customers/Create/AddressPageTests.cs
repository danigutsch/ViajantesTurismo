using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class AddressPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public AddressPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_when_state_already_has_address_preloads_existing_values()
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
        (cut.Find("#street").GetAttribute("value")).ShouldBe("Rua das Flores, 123");
        (cut.Find("#complement").GetAttribute("value")).ShouldBe("Apt 45");
        (cut.Find("#neighborhood").GetAttribute("value")).ShouldBe("Centro");
        (cut.Find("#postalCode").GetAttribute("value")).ShouldBe("01000-000");
        (cut.Find("#city").GetAttribute("value")).ShouldBe("São Paulo");
        (cut.Find("#state").GetAttribute("value")).ShouldBe("SP");
        (cut.Find("#country").GetAttribute("value")).ShouldBe("Brazil");
        (_state.CurrentStep).ShouldBe(4);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_physical()
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
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/physical", StringComparison.Ordinal));
        (_state.Address).ShouldNotBeNull();
        (_state.Address!.Street).ShouldBe("Rua das Flores, 123");
        (_state.Address.Complement).ShouldBe("Apt 45");
        (_state.Address.Neighborhood).ShouldBe("Centro");
        (_state.Address.PostalCode).ShouldBe("01000-000");
        (_state.Address.City).ShouldBe("São Paulo");
        (_state.Address.State).ShouldBe("SP");
        (_state.Address.Country).ShouldBe("Brazil");
        (_state.CurrentStep).ShouldBe(5);
    }

    [Fact]
    public async Task Back_button_navigates_to_contact_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Address>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/contact", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(3);
    }
}
