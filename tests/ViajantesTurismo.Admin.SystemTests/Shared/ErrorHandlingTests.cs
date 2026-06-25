namespace ViajantesTurismo.Admin.SystemTests.Shared;

/// <summary>
/// E2E smoke tests that require an isolated database baseline to verify empty-state UI behavior.
/// </summary>
public class ErrorHandlingTests(IsolatedAspireSystemTestFixture fixture)
    : AspireSystemTestBase<IsolatedAspireSystemTestFixture>(fixture), IClassFixture<IsolatedAspireSystemTestFixture>
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await Fixture.ResetToKnownBaseline(cts.Token);
    }

    [Fact]
    [SerialE2EReason(
        "/customers renders the empty-state branch only when the API returns zero customers; any " +
        "owned-data setup or concurrent customer creation invalidates the behavior under test.")]
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
