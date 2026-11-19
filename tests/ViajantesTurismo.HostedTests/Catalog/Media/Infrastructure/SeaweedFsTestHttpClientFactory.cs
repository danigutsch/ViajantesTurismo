using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Amazon.Runtime;

namespace ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

/// <summary>Creates HTTP clients with the isolated SeaweedFS test CA and regular hostname verification.</summary>
internal sealed class SeaweedFsTestHttpClientFactory : HttpClientFactory, IDisposable
{
    private readonly SocketsHttpHandler handler;

    /// <summary>Creates a handler that trusts the supplied test authority.</summary>
    public SeaweedFsTestHttpClientFactory(X509Certificate2 authority)
    {
        var chain = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            // The temporary test authority is discarded after the scenario and publishes no CRL.
            RevocationMode = X509RevocationMode.NoCheck,
        };
        chain.CustomTrustStore.Add(authority);
        handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            SslOptions = new SslClientAuthenticationOptions { CertificateChainPolicy = chain },
        };
    }

    /// <summary>Creates a client using the shared test handler.</summary>
    public HttpClient CreateClient() => new(handler, disposeHandler: false);

    /// <inheritdoc/>
    public override HttpClient CreateHttpClient(IClientConfig clientConfig) => CreateClient();

    /// <inheritdoc/>
    public override bool UseSDKHttpClientCaching(IClientConfig clientConfig) => false;

    /// <inheritdoc/>
    public void Dispose() => handler.Dispose();
}
