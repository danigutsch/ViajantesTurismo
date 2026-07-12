namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Bases;

[Collection(IntegrationTestCollections.Serial)]
public abstract class AspireSerialIntegrationTestBase(
    ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan DatabaseResetTimeout = TimeSpan.FromSeconds(30);

    protected HttpClient Client => fixture.Client;

    protected Uri BaseUri => fixture.BaseUri;

    public virtual async ValueTask InitializeAsync()
    {
        await ResetToKnownBaseline();
    }

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task ResetToKnownBaseline()
    {
        using var cts = new CancellationTokenSource(DatabaseResetTimeout);
        await fixture.ResetToKnownBaseline(cts.Token);
    }
}
