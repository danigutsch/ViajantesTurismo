using System.Security.Claims;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class InflightCachedAudienceBackendSend : IAsyncDisposable
{
    private readonly BlockingFirstRemoveDistributedCache cache;
    private readonly AudienceTokenExchangeIntegrationTestScope exchange;
    private readonly Task<HttpResponseMessage> firstResponse;
    private readonly Task<bool> signOut;

    private InflightCachedAudienceBackendSend(
        BlockingFirstRemoveDistributedCache cache,
        AudienceTokenExchangeIntegrationTestScope exchange,
        Task<HttpResponseMessage> firstResponse,
        Task<bool> signOut)
    {
        this.cache = cache;
        this.exchange = exchange;
        this.firstResponse = firstResponse;
        this.signOut = signOut;
    }

    public IReadOnlyList<string?> AuthorizationHeaders => exchange.AuthorizationHeaders;

    public static async Task<InflightCachedAudienceBackendSend> Start(
        PostgreSqlManagementUserTokenStoreScenario scenario,
        ClaimsPrincipal user,
        string sourceAccessToken,
        string exchangedAccessToken,
        CancellationToken ct)
    {
        var cache = scenario.CreateBlockingFirstRemoveCache();
        var signingOutStore = scenario.CreateStore(cache);
        var userTokenStore = scenario.CreateStore();
        var audienceTokenStore = scenario.CreateAudienceTokenStore();
        await audienceTokenStore.Store(
            ApiAudienceNames.Admin,
            ManagementTokenSession.From(user),
            sourceAccessToken,
            exchangedAccessToken,
            DateTimeOffset.UtcNow.AddMinutes(5),
            ct);
        var exchange = AudienceTokenExchangeIntegrationTestScope.Create(user, userTokenStore, audienceTokenStore);
        var firstResponse = exchange.Send(
            new Uri("https://admin.example.test/first"),
            sourceAccessToken,
            ct);
        await exchange.WaitForBackendSend(ct);
        var signOut = signingOutStore.ClearAll(user, ct);

        return new InflightCachedAudienceBackendSend(cache, exchange, firstResponse, signOut);
    }

    public Task WaitForSignOutToReachRemove(CancellationToken ct)
    {
        return cache.WaitForFirstRemove(ct);
    }

    public async Task CompleteSignOut(CancellationToken ct)
    {
        cache.ReleaseFirstRemove();
        await signOut.WaitAsync(ct);
    }

    public Task<HttpResponseMessage> SendAfterSignOut(string sourceAccessToken, CancellationToken ct)
    {
        return exchange.Send(new Uri("https://admin.example.test/later"), sourceAccessToken, ct);
    }

    public async Task<HttpResponseMessage> CompleteFirstBackendSend(CancellationToken ct)
    {
        exchange.ReleaseBackendSend();
        return await firstResponse.WaitAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        cache.ReleaseFirstRemove();
        exchange.ReleaseBackendSend();

        using var response = await firstResponse;
        _ = await signOut;

        exchange.Dispose();
    }
}
