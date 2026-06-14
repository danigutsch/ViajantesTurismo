namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Bases;

[Collection(IntegrationTestCollections.Serial)]
public abstract class AspireSerialIntegrationTestBase(AspireSerialIntegrationTestFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    protected HttpClient Client => fixture.Client;

    protected Uri BaseUri => fixture.BaseUri;

    public virtual async ValueTask InitializeAsync()
    {
        await ResetDatabase();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await ResetDatabase();
        GC.SuppressFinalize(this);
    }

    private async Task ResetDatabase()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ResetDatabase(cts.Token);
    }
}
