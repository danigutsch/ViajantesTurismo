using Microsoft.AspNetCore.Components;
using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;
using ViajantesTurismo.Admin.Web.Models;

namespace ViajantesTurismo.Admin.WebTests.Components.Pages.Customers.Create;

public sealed class AccommodationPageTests : BunitContext
{
    private readonly FakeCustomersApiClient _fakeCustomersApi = new();
    private readonly CustomerCreationState _state = new();

    public AccommodationPageTests()
    {
        Services.AddSingleton<ICustomersApiClient>(_fakeCustomersApi);
        Services.AddSingleton(_state);
    }

    [Fact]
    public async Task Does_Not_Render_Customer_Search_Input_In_Accommodation_Wizard_Step()
    {
        // Arrange
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(firstName: "Alice", lastName: "Brown", email: "alice@example.com"));

        // Act
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.Empty(cut.FindAll("input[placeholder='Search customers by name or email...']"));
    }

    [Fact]
    public async Task OnInitialized_When_State_Already_Has_AccommodationPreferences_Preloads_Existing_Values()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(
            id: companionId,
            firstName: "Alice",
            lastName: "Brown",
            email: "alice@example.com"));
        _state.SetAccommodationPreferences(new AccommodationPreferencesFormModel
        {
            RoomType = RoomTypeDto.SingleOccupancy,
            BedType = BedTypeDto.DoubleBed,
            CompanionId = companionId,
        });

        // Act
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.Equal(nameof(RoomTypeDto.SingleOccupancy), cut.Find("#roomType").GetAttribute("value"));
        Assert.Equal(nameof(BedTypeDto.DoubleBed), cut.Find("#bedType").GetAttribute("value"));
        Assert.Contains("alice@example.com", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(6, _state.CurrentStep);
    }

    [Fact]
    public async Task Submit_When_Form_Is_Valid_Saves_State_And_Navigates_To_Emergency_Contact()
    {
        // Arrange
        var companionId = Guid.NewGuid();
        _fakeCustomersApi.AddCustomer(BuildCustomerDto(
            id: companionId,
            firstName: "Alice",
            lastName: "Brown",
            email: "alice@example.com"));
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        await cut.InvokeAsync(() => cut.Find("#roomType").Change(nameof(RoomTypeDto.DoubleOccupancy)));
        await cut.InvokeAsync(() => cut.Find("#bedType").Change(nameof(BedTypeDto.SingleBed)));
        var selects = cut.FindAll("select.form-select");
        await cut.InvokeAsync(() => selects[selects.Count - 1].Change(companionId.ToString()));
        await cut.InvokeAsync(async () => await cut.Find("form").SubmitAsync());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/emergency-contact", navigationManager.Uri, StringComparison.Ordinal));
        Assert.NotNull(_state.AccommodationPreferences);
        Assert.Equal(RoomTypeDto.DoubleOccupancy, _state.AccommodationPreferences!.RoomType);
        Assert.Equal(BedTypeDto.SingleBed, _state.AccommodationPreferences.BedType);
        Assert.Equal(companionId, _state.AccommodationPreferences.CompanionId);
        Assert.Equal(7, _state.CurrentStep);
    }

    [Fact]
    public async Task Back_Button_Navigates_To_Physical_And_Updates_Current_Step()
    {
        // Arrange
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        var cut = Render<Accommodation>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Act
        var backButton = cut.FindAll("button").First(button => button.TextContent.Contains("Back", StringComparison.Ordinal));
        await cut.InvokeAsync(() => backButton.Click());

        // Assert
        await cut.WaitForAssertionAsync(() => Assert.EndsWith("/customers/create/physical", navigationManager.Uri, StringComparison.Ordinal));
        Assert.Equal(5, _state.CurrentStep);
    }
}
