using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Management.Web;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Management.WebIntegrationTests;

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

    public IReadOnlyList<string?> AuthorizationHeaders => Backend.AuthorizationHeaders;

    private BlockingBackendHandler Backend { get; }

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

    public async Task<HttpResponseMessage> Send(Uri requestUri, string sourceAccessToken, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAccessToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sourceAccessToken);
        return await _client.SendAsync(request, ct);
    }

    public Task WaitForBackendSend(CancellationToken ct)
    {
        return Backend.WaitForSend(ct);
    }

    public void ReleaseBackendSend()
    {
        Backend.ReleaseSend();
    }

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

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
            _sendStarted.TrySetResult();
            await _releaseSend.Task.WaitAsync(cancellationToken);
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
        public Task<ClaimsPrincipal> GetCurrentUserAsync(CancellationToken ct = default)
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
