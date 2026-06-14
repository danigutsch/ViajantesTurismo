namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Bases;

[Collection(ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures.IntegrationTestCollections.Serial)]
public abstract class AspireSerialIntegrationTestBase(
    ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures.AspireSerialIntegrationTestFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    protected HttpClient Client => fixture.Client;

    protected Uri BaseUri => fixture.BaseUri;

    public virtual async ValueTask InitializeAsync()
    {
        await ResetDatabase();
    }

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task ResetDatabase()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ResetDatabase(cts.Token);
    }
}
