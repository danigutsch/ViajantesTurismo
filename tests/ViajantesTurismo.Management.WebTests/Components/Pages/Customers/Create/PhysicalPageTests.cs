using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Management.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Management.Web.Models;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Customers.Create;

public sealed class PhysicalPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public PhysicalPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_when_state_already_has_physical_info_preloads_existing_values()
    {
        // Arrange
        _state.SetPhysicalInfo(new PhysicalInfoFormModel
        {
            WeightKg = 72.5m,
            HeightCentimeters = 181,
            BikeType = BikeTypeDto.EBike,
        });

        // Act
        var cut = Render<Physical>();

        // Assert
        (cut.Find("#weightKg").GetAttribute("value")).ShouldBe("72.5");
        (cut.Find("#heightCm").GetAttribute("value")).ShouldBe("181");
        (cut.Find("#bikeType").GetAttribute("value")).ShouldBe(nameof(BikeTypeDto.EBike));
        (_state.CurrentStep).ShouldBe(5);
    }

    [Fact]
    public async Task Submit_when_form_is_valid_saves_state_and_navigates_to_accommodation()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Physical>();

        // Act
        await cut.InvokeAsync(() => cut.Find("#weightKg").Change("68.4"));
        await cut.InvokeAsync(() => cut.Find("#heightCm").Change("175"));
        await cut.InvokeAsync(() => cut.Find("#bikeType").Change(nameof(BikeTypeDto.Regular)));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/accommodation", StringComparison.Ordinal));
        (_state.PhysicalInfo).ShouldNotBeNull();
        (_state.PhysicalInfo!.WeightKg).ShouldBe(68.4m);
        (_state.PhysicalInfo.HeightCentimeters).ShouldBe(175);
        (_state.PhysicalInfo.BikeType).ShouldBe(BikeTypeDto.Regular);
        (_state.CurrentStep).ShouldBe(6);
    }

    [Fact]
    public async Task Back_button_navigates_to_address_and_updates_current_step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Physical>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => (navigationManager.Uri).ShouldEndWith("/customers/create/address", StringComparison.Ordinal));
        (_state.CurrentStep).ShouldBe(4);
    }
}
