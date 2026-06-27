using Aspire.Hosting.Testing;

namespace SharedKernel.Testing;

/// <summary>
/// Owns an Aspire distributed application lifetime for integration tests.
/// </summary>
public sealed class AspireTestApplication : IAsyncDisposable
{
    /// <summary>
    /// Gets the default timeout for resource startup waits.
    /// </summary>
    public static readonly TimeSpan DefaultResourceStartupTimeout = TimeSpan.FromSeconds(90);

    private IDistributedApplicationTestingBuilder? _appBuilder;
    private DistributedApplication? _app;

    private AspireTestApplication(IDistributedApplicationTestingBuilder appBuilder, DistributedApplication app)
    {
        _appBuilder = appBuilder;
        _app = app;
    }

    /// <summary>
    /// Starts an Aspire application and waits for the requested resources to become healthy.
    /// </summary>
    /// <typeparam name="TAppHost">The AppHost entry-point type.</typeparam>
    /// <param name="healthyResourceNames">Resource names that must become healthy before the method returns.</param>
    /// <param name="resourceStartupTimeout">The resource startup timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The started test application.</returns>
    public static async Task<AspireTestApplication> Start<TAppHost>(
        IEnumerable<string> healthyResourceNames,
        CancellationToken ct,
        TimeSpan? resourceStartupTimeout = null)
        where TAppHost : class
    {
        ArgumentNullException.ThrowIfNull(healthyResourceNames);

        IDistributedApplicationTestingBuilder? appBuilder = null;
        DistributedApplication? app = null;

        try
        {
            appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<TAppHost>(ct);
            app = await appBuilder.BuildAsync(ct);
            await app.StartAsync(ct);

            using var timeoutCts = new CancellationTokenSource(resourceStartupTimeout ?? DefaultResourceStartupTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            foreach (var resourceName in healthyResourceNames)
            {
                await app.ResourceNotifications.WaitForResourceHealthyAsync(resourceName, linkedCts.Token);
            }

            return new AspireTestApplication(appBuilder, app);
        }
        catch
        {
            try
            {
                if (app is not null)
                {
                    await app.StopAsync(CancellationToken.None);
                    await app.DisposeAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Preserve the original startup exception.
            }
            catch (InvalidOperationException)
            {
                // Preserve the original startup exception.
            }

            try
            {
                if (appBuilder is not null)
                {
                    await appBuilder.DisposeAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Preserve the original startup exception.
            }
            catch (InvalidOperationException)
            {
                // Preserve the original startup exception.
            }

            throw;
        }
    }

    /// <summary>
    /// Creates an HTTP client for an Aspire resource.
    /// </summary>
    /// <param name="resourceName">The Aspire resource name.</param>
    /// <returns>An HTTP client for the resource.</returns>
    public HttpClient CreateHttpClient(string resourceName)
    {
        return App.CreateHttpClient(resourceName);
    }

    /// <summary>
    /// Gets the endpoint URI for an Aspire resource.
    /// </summary>
    /// <param name="resourceName">The Aspire resource name.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <returns>The endpoint URI.</returns>
    public Uri GetEndpoint(string resourceName, string endpointName)
    {
        return App.GetEndpoint(resourceName, endpointName);
    }

    /// <summary>
    /// Gets a connection string for an Aspire resource.
    /// </summary>
    /// <param name="resourceName">The Aspire resource name.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The configured connection string.</returns>
    public async Task<string> GetConnectionString(string resourceName, CancellationToken ct)
    {
        return await App.GetConnectionStringAsync(resourceName, ct)
            ?? throw new InvalidOperationException($"{resourceName} connection string is not configured.");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var app = _app;
        var appBuilder = _appBuilder;
        _app = null;
        _appBuilder = null;

        try
        {
            if (app is not null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
        finally
        {
            if (appBuilder is not null)
            {
                await appBuilder.DisposeAsync();
            }
        }
    }

    private DistributedApplication App => _app ?? throw new InvalidOperationException("Aspire test application is not initialized.");
}
