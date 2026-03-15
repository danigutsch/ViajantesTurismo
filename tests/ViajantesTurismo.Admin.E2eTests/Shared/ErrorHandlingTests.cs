namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class ErrorHandlingTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Show_Destructive_Reset_Empty_State_Smoke_On_Customers_List()
    {
        // Arrange
        // Clear the database to test empty states (base class seeds by default)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await ClearDatabase(cts.Token);

        // Act
        await NavigateTo("/customers");
        await Expect(Page).ToHaveTitleAsync("Customers");

        // Assert
        await Expect(Page.GetByText("No customers found")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Create your first customer")).ToBeVisibleAsync();
    }
}
