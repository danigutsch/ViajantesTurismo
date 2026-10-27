using System.Security.Claims;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>Arranges a backend send in flight while a concurrent sign-out is blocked on its cache removal.</summary>
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

    /// <summary>Authorization headers recorded by the backend for each send.</summary>
    public IReadOnlyList<string?> AuthorizationHeaders => exchange.AuthorizationHeaders;

    /// <summary>Stores an audience token, starts a held backend send, then starts a sign-out that is blocked on cache removal.</summary>
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
            TimeProvider.System.GetUtcNow().AddMinutes(5),
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

    /// <summary>Completes once the sign-out has reached its first cache removal.</summary>
    public Task WaitForSignOutToReachRemove(CancellationToken ct)
    {
        return cache.WaitForFirstRemove(ct);
    }

    /// <summary>Releases the blocked removal and waits for the sign-out to finish.</summary>
    public async Task CompleteSignOut(CancellationToken ct)
    {
        cache.ReleaseFirstRemove();
        await signOut.WaitAsync(ct);
    }

    /// <summary>Sends a new backend request after sign-out using the given source access token.</summary>
    public Task<HttpResponseMessage> SendAfterSignOut(string sourceAccessToken, CancellationToken ct)
    {
        return exchange.Send(new Uri("https://admin.example.test/later"), sourceAccessToken, ct);
    }

    /// <summary>Releases the held backend send and returns the first response.</summary>
    public async Task<HttpResponseMessage> CompleteFirstBackendSend(CancellationToken ct)
    {
        exchange.ReleaseBackendSend();
        return await firstResponse.WaitAsync(ct);
    }

    /// <summary>Releases all held operations and awaits the pending send and sign-out before disposing the exchange scope.</summary>
    public async ValueTask DisposeAsync()
    {
        cache.ReleaseFirstRemove();
        exchange.ReleaseBackendSend();

        using var response = await firstResponse;
        _ = await signOut;

        exchange.Dispose();
    }
}
