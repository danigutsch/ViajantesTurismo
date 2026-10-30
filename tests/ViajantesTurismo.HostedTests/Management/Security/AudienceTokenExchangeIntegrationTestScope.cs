using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Test scope that runs the Keycloak audience token exchange handler against a blocking fake backend.</summary>
internal sealed class AudienceTokenExchangeIntegrationTestScope : IDisposable
{
    private readonly HttpMessageInvoker _client;
    private readonly KeycloakAudienceTokenExchangeHandler _exchangeHandler;
    private readonly ServiceProvider _serviceProvider;

    private AudienceTokenExchangeIntegrationTestScope(
        HttpMessageInvoker client,
        KeycloakAudienceTokenExchangeHandler exchangeHandler,
        ServiceProvider serviceProvider,
        BlockingBackendHandler backend)
    {
        _client = client;
        _exchangeHandler = exchangeHandler;
        _serviceProvider = serviceProvider;
        Backend = backend;
    }

    /// <summary>Gets the Authorization header values the fake backend received.</summary>
    public IReadOnlyList<string?> AuthorizationHeaders => Backend.AuthorizationHeaders;

    private BlockingBackendHandler Backend
    {
        get;
    }

    /// <summary>Creates a scope for the user with the given token stores and a cached-token-only exchange handler.</summary>
    public static AudienceTokenExchangeIntegrationTestScope Create(
        ClaimsPrincipal user,
        ProtectedDistributedUserTokenStore userTokenStore,
        ProtectedDistributedAudienceTokenStore audienceTokenStore)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(userTokenStore);
        ArgumentNullException.ThrowIfNull(audienceTokenStore);

        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, _ => { });
        var serviceProvider = services.BuildServiceProvider();
        var backend = new BlockingBackendHandler();
        var exchangeHandler = new KeycloakAudienceTokenExchangeHandler(
            ApiAudienceNames.Admin,
            new ThrowingHttpClientFactory(),
            audienceTokenStore,
            serviceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>(),
            new FixedUserAccessor(user),
            userTokenStore,
            TimeProvider.System)
        {
            InnerHandler = backend
        };

        return new AudienceTokenExchangeIntegrationTestScope(
            new HttpMessageInvoker(exchangeHandler, disposeHandler: false),
            exchangeHandler,
            serviceProvider,
            backend);
    }

    /// <summary>Sends a GET with the bearer source token through the exchange handler.</summary>
    public async Task<HttpResponseMessage> Send(Uri requestUri, string sourceAccessToken, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sourceAccessToken);
        return await _client.SendAsync(request, ct);
    }

    /// <summary>Waits until the fake backend has received a request.</summary>
    public Task WaitForBackendSend(CancellationToken ct)
    {
        return Backend.WaitForSend(ct);
    }

    /// <summary>Lets the blocked backend request complete.</summary>
    public void ReleaseBackendSend()
    {
        Backend.ReleaseSend();
    }

    /// <summary>Disposes the client, the exchange handler and the service provider.</summary>
    public void Dispose()
    {
        _client.Dispose();
        _exchangeHandler.Dispose();
        _serviceProvider.Dispose();
    }

    private sealed class BlockingBackendHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _releaseSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _sendStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string?> AuthorizationHeaders { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
            _sendStarted.TrySetResult();
            await _releaseSend.Task.WaitAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public void ReleaseSend()
        {
            _releaseSend.TrySetResult();
        }

        public Task WaitForSend(CancellationToken ct)
        {
            return _sendStarted.Task.WaitAsync(ct);
        }
    }

    private sealed class FixedUserAccessor(ClaimsPrincipal user) : IUserAccessor
    {
        public Task<ClaimsPrincipal> GetCurrentUserAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(user);
        }
    }

    private sealed class ThrowingHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            throw new InvalidOperationException("The cached audience token should avoid token exchange.");
        }
    }
}
