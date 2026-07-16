using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using SharedKernel.IntegrationTesting;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebIntegrationTests;

internal sealed class PostgreSqlManagementUserTokenStoreScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "managementsecurity";
    private const string CacheSchemaName = "public";
    private const string CacheTableName = "user_token_store_test_cache";

    private AspireTestApplication? _app;
    private BlockingFirstSetDistributedCache? _blockingCache;
    private IDistributedCache? _cache;
    private IDataProtectionProvider? _dataProtectionProvider;
    private NpgsqlDataSource? _dataSource;
    private ServiceProvider? _serviceProvider;

    public BlockingFirstSetDistributedCache BlockingCache => _blockingCache is not null
        ? _blockingCache
        : throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");

    public async ValueTask InitializeAsync()
    {
        var appBuilder = DistributedApplication.CreateBuilder([]);
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        _app = await AspireTestApplication.Start(
            appBuilder,
            [PostgreSqlResourceName],
            null,
            TestContext.Current.CancellationToken);
        var connectionString = await _app.GetConnectionString(DatabaseResourceName, TestContext.Current.CancellationToken);
        _dataSource = NpgsqlDataSource.Create(connectionString);

        var services = new ServiceCollection();
        services.AddDistributedPostgresCache(options =>
        {
            options.ConnectionString = connectionString;
            options.SchemaName = CacheSchemaName;
            options.TableName = CacheTableName;
            options.CreateIfNotExists = true;
            options.UseWAL = true;
        });
        _serviceProvider = services.BuildServiceProvider();
        _cache = _serviceProvider.GetRequiredService<IDistributedCache>();
        _blockingCache = new BlockingFirstSetDistributedCache(_cache);
        _dataProtectionProvider = new EphemeralDataProtectionProvider();
    }

    public ProtectedDistributedUserTokenStore CreateStore()
    {
        var cache = _cache ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        return CreateStore(cache);
    }

    public ProtectedDistributedUserTokenStore CreateStore(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        var dataProtectionProvider = _dataProtectionProvider
            ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        var dataSource = _dataSource
            ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");

        return new ProtectedDistributedUserTokenStore(cache, dataProtectionProvider, TimeProvider.System, dataSource);
    }

    public BlockingFirstGetDistributedCache CreateBlockingFirstGetCache(string sessionId)
    {
        var cache = _cache ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        var cacheKey = string.Concat(ManagementAuthenticationDefaults.UserTokenStoreKeyPrefix, sessionId);
        return new BlockingFirstGetDistributedCache(cache, cacheKey);
    }

    public BlockingFirstRemoveDistributedCache CreateBlockingFirstRemoveCache()
    {
        var cache = _cache ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        return new BlockingFirstRemoveDistributedCache(cache);
    }

    public static ClaimsPrincipal CreateUser(string sessionId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, sessionId),
            new Claim(
                ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType,
                DateTimeOffset.UtcNow.Add(ManagementAuthenticationDefaults.SessionLifetime).ToString("O")),
        ],
        "test");

        return new ClaimsPrincipal(identity);
    }

    public static UserToken CreateToken(string accessToken)
    {
        return new UserToken
        {
            AccessToken = AccessToken.Parse(accessToken),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            ClientId = ClientId.Parse("web-app"),
            Expiration = DateTimeOffset.UtcNow.AddMinutes(5),
            RefreshToken = RefreshToken.Parse("refresh-token"),
        };
    }

    public Task SetCorruptEntry(string sessionId, CancellationToken ct)
    {
        var cache = _cache ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        var cacheKey = string.Concat(ManagementAuthenticationDefaults.UserTokenStoreKeyPrefix, sessionId);
        return cache.SetAsync(
            cacheKey,
            [0x01],
            new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(ManagementAuthenticationDefaults.SessionLifetime) },
            ct);
    }

    public async Task WaitForWaitingAdvisoryLock(CancellationToken ct)
    {
        var dataSource = _dataSource ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            while (!linkedCts.IsCancellationRequested)
            {
                await using var connection = await dataSource.OpenConnectionAsync(linkedCts.Token);
                await using var command = new NpgsqlCommand(
                    "SELECT EXISTS (SELECT 1 FROM pg_locks WHERE locktype = 'advisory' AND NOT granted);",
                    connection);
                var waitingLock = await command.ExecuteScalarAsync(linkedCts.Token);
                if (waitingLock is true)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException("The second user-token mutation did not wait for the PostgreSQL advisory lock.");
        }

        throw new TimeoutException("The second user-token mutation did not wait for the PostgreSQL advisory lock.");
    }

    public async Task ReleaseFirstSetAfterWaitingForLock(BlockingFirstSetDistributedCache cache, CancellationToken ct)
    {
        try
        {
            await WaitForWaitingAdvisoryLock(ct);
        }
        finally
        {
            cache.ReleaseFirstSet();
        }
    }

    public async Task ReleaseFirstRemoveAfterWaitingForLock(
        BlockingFirstRemoveDistributedCache cache,
        Task remove,
        CancellationToken ct)
    {
        try
        {
            await WaitForWaitingAdvisoryLock(ct);
        }
        finally
        {
            cache.ReleaseFirstRemove();
        }

        await remove;
    }

    public async ValueTask DisposeAsync()
    {
        var serviceProvider = _serviceProvider;
        var dataSource = _dataSource;
        var app = _app;
        _serviceProvider = null;
        _dataSource = null;
        _dataProtectionProvider = null;
        _blockingCache = null;
        _cache = null;
        _app = null;

        if (serviceProvider is not null)
        {
            await serviceProvider.DisposeAsync();
        }

        if (dataSource is not null)
        {
            await dataSource.DisposeAsync();
        }

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
