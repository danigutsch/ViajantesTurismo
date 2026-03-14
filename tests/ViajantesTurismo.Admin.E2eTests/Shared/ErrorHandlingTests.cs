namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class ErrorHandlingTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_Show_Empty_States_On_All_List_Pages()
    {
        // Arrange
        // Clear the database to test empty states (base class seeds by default)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await ClearDatabase(cts.Token);

        // Act
        await NavigateTo("/tours");

        // Assert
        await Expect(Page.GetHeading("Tours")).ToBeVisibleAsync();
        await Expect(Page.Locator("table tbody tr")).ToHaveCountAsync(0);

        // Act
        await NavigateTo("/customers");

        // Assert
        await Expect(Page.GetByText("No customers found")).ToBeVisibleAsync();
        await Expect(Page.GetLink("Create your first customer")).ToBeVisibleAsync();

        // Act
        await NavigateTo("/bookings");

        // Assert
        await Expect(Page.GetByText("No bookings found")).ToBeVisibleAsync();
    }
}
