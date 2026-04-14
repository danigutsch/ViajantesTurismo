using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class PhysicalPageTests : BunitContext
{
    private readonly CustomerCreationState _state = new();

    public PhysicalPageTests()
    {
        Services.AddSingleton(_state);
    }

    [Fact]
    public void OnInitialized_When_State_Already_Has_Physical_Info_Preloads_Existing_Values()
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
        Assert.Equal("72.5", cut.Find("#weightKg").GetAttribute("value"));
        Assert.Equal("181", cut.Find("#heightCm").GetAttribute("value"));
        Assert.Equal(nameof(BikeTypeDto.EBike), cut.Find("#bikeType").GetAttribute("value"));
        Assert.Equal(5, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Accommodation()
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
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/accommodation", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.PhysicalInfo);
        Assert.Equal(68.4m, _state.PhysicalInfo!.WeightKg);
        Assert.Equal(175, _state.PhysicalInfo.HeightCentimeters);
        Assert.Equal(BikeTypeDto.Regular, _state.PhysicalInfo.BikeType);
        Assert.Equal(6, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_Button_Navigates_To_Address_And_Updates_Current_Step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Physical>();

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/address", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(4, _state.CurrentStep);
    }
}
