using ViajantesTurismo.Admin.Web;
using ViajantesTurismo.Admin.Web.Components.Pages.Customers.Create;

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
}
