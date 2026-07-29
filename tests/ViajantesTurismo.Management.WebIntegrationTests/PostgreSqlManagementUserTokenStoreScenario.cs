using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebIntegrationTests;

[SuppressMessage(
    "Usage",
    "CA2213:Disposable fields should be disposed",
    Justification = "DisposeAsync passes the provider and data source to the ordered independent cleanup helper.")]
internal sealed class PostgreSqlManagementUserTokenStoreScenario : IAsyncDisposable
{
    private const string CacheSchemaName = "public";
    private const string CacheTableName = "user_token_store_test_cache";

    private BlockingFirstSetDistributedCache? _blockingCache;
    private IDistributedCache? _cache;
    private IDataProtectionProvider? _dataProtectionProvider;
    private PostgreSqlTestDatabase? _database;
    private NpgsqlDataSource? _dataSource;
    private readonly PostgreSqlTestServerFixture _fixture;
    private ServiceProvider? _serviceProvider;

    internal PostgreSqlManagementUserTokenStoreScenario(PostgreSqlTestServerFixture fixture)
    {
        _fixture = fixture;
    }

    public BlockingFirstSetDistributedCache BlockingCache => _blockingCache is not null
        ? _blockingCache
        : throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Failed setup must attempt every cleanup and preserve all failures.")]
    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Successful setup transfers database, data-source, and provider ownership to the scenario.")]
    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = await _fixture.CreateDatabase(ct);
        NpgsqlDataSource? dataSource = null;
        ServiceProvider? serviceProvider = null;
        try
        {
            dataSource = NpgsqlDataSource.Create(database.ConnectionString);
            var services = new ServiceCollection();
            services.AddDistributedPostgresCache(options =>
            {
                options.ConnectionString = database.ConnectionString;
                options.SchemaName = CacheSchemaName;
                options.TableName = CacheTableName;
                options.CreateIfNotExists = true;
                options.UseWAL = true;
            });
            serviceProvider = services.BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<IDistributedCache>();
            var blockingCache = new BlockingFirstSetDistributedCache(cache);
            var dataProtectionProvider = new EphemeralDataProtectionProvider();

            _database = database;
            _dataSource = dataSource;
            _serviceProvider = serviceProvider;
            _cache = cache;
            _blockingCache = blockingCache;
            _dataProtectionProvider = dataProtectionProvider;
        }
        catch (Exception creationFailure)
        {
            await PostgreSqlTestCleanup.DisposeResources(creationFailure, serviceProvider, dataSource, database);
            throw;
        }
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

    public ProtectedDistributedAudienceTokenStore CreateAudienceTokenStore()
    {
        var cache = _cache ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");
        var dataProtectionProvider = _dataProtectionProvider
            ?? throw new InvalidOperationException("The PostgreSQL user-token-store test scenario has not started.");

        return new ProtectedDistributedAudienceTokenStore(cache, dataProtectionProvider, TimeProvider.System);
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

        ct.ThrowIfCancellationRequested();
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
        var database = _database;
        _serviceProvider = null;
        _dataSource = null;
        _database = null;
        _dataProtectionProvider = null;
        _blockingCache = null;
        _cache = null;

        await PostgreSqlTestCleanup.DisposeResources(null, serviceProvider, dataSource, database);
    }
}
