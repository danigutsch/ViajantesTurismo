namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class ErrorHandlingTests(AspireSerialSystemTestFixture fixture) : AspireSerialSystemTestBase(fixture)
{
    [Fact]
    public async Task Can_Show_Destructive_Reset_Empty_State_Smoke_On_Customers_List()
    {
        // Act
        await NavigateTo("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        // Assert
        await Expect(Page.GetByText("No customers found")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Create your first customer")).ToBeVisibleAsync();
    }
}
