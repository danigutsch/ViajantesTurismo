using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Amazon.Runtime;
using Amazon.S3;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;

namespace ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

/// <summary>Owns temporary SeaweedFS and Keycloak containers and their synthetic trust configuration.</summary>
internal sealed class SeaweedFsWebIdentityScenario : IAsyncDisposable
{
    /// <summary>Gets the dedicated bucket provisioned at container startup.</summary>
    public const string Bucket = "media-identity";
    private const string Storage = "seaweedfs-tls";
    private const string IdentityProvider = "media-issuer";
    private const string Realm = "media-storage";
    private const string ClientId = "catalog-media-storage";
    private const string WriterRole = "catalog-media-writer";
    private const string RoleArn = "arn:aws:iam::role/" + WriterRole;
    private const string Issuer = "https://" + IdentityProvider + ":8443/realms/" + Realm;
    private const string RealmPath = "/realms/" + Realm;
    private const string SeaweedConfig = "/etc/seaweedfs";
    private const string IdentityConfig = "/opt/keycloak/data/import";
    private const string HttpsEndpoint = "https";
    private static readonly string[] AssumeRoleActions = ["sts:AssumeRoleWithWebIdentity"];
    private static readonly string[] MediaObjectActions = ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"];
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly XNamespace StsNamespace = "https://sts.amazonaws.com/doc/2011-06-15/";

    private readonly AspireTestApplication application;
    private readonly X509Certificate2 authority;
    private readonly SeaweedFsTestHttpClientFactory clientFactory;
    private readonly string workloadSecret;

    private SeaweedFsWebIdentityScenario(AspireTestApplication application, X509Certificate2 authority, string workloadSecret)
    {
        this.application = application;
        this.authority = authority;
        this.workloadSecret = workloadSecret;
        clientFactory = new SeaweedFsTestHttpClientFactory(authority);
    }

    /// <summary>Gets the storage endpoint on the Aspire loopback proxy.</summary>
    public Uri Endpoint => LocalEndpoint(Storage);

    /// <summary>Creates a client for HTTPS requests that use the test authority.</summary>
    public HttpClient CreateHttpClient() => clientFactory.CreateClient();

    /// <summary>Starts the pinned provider images.</summary>
    public static async Task<SeaweedFsWebIdentityScenario> Start(TimeProvider timeProvider, CancellationToken ct)
    {
        using var signingKey = RSA.Create(2048);
        var request = new CertificateRequest("CN=VT hosted media test CA", signingKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, critical: true));
        var now = timeProvider.GetUtcNow();
        var authority = request.CreateSelfSigned(now.AddMinutes(-5), now.AddHours(1));
        try
        {
            var workloadSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
            var builder = AspireTestApplication.CreateBuilder([.. AppHostTestArguments.CreateConfiguration().Arguments]);

            // Use the pinned AppHost Keycloak image, with a separate realm and trust root for this test.
            var identity = builder.AddContainer(IdentityProvider, "keycloak/keycloak", "26.7.0")
                .WithImageRegistry("quay.io")
                .WithImageSHA256("2eb3cd316835c990e69e26ade292ffa78f6fb0db7d5fc6377463c162e1979ac0")
                .WithContainerRuntimeArgs("--log-driver=json-file")
                .WithContainerFiles(IdentityConfig, [
                    .. TlsFiles(IdentityProvider, authority, now),
                    new ContainerFile { Name = "media-storage-realm.json", Contents = RealmConfiguration(workloadSecret), Mode = UnixFileMode.UserRead },
                ], defaultOwner: 1000)
                .WithArgs("start-dev", "--import-realm", "--http-enabled=false",
                    "--hostname=https://" + IdentityProvider + ":8443",
                    "--https-certificate-file=" + IdentityConfig + "/tls.crt",
                    "--https-certificate-key-file=" + IdentityConfig + "/tls.key")
                .WithHttpsEndpoint(targetPort: 8443);

            // Match the SharedKernel 4.39 pin while composing the TLS-only endpoint and IAM configuration.
            _ = builder.AddContainer(Storage, "chrislusf/seaweedfs", "4.39")
                .WithImageRegistry("docker.io")
                .WithImageSHA256("c7d6c721b30ae711db766bbbfd40192776e263d4e51e22f57baef7bef93c12c6")
                .WithContainerRuntimeArgs("--log-driver=json-file")
                .WithContainerFiles(SeaweedConfig, [
                    .. TlsFiles(Storage, authority, now),
                    new ContainerFile { Name = "issuer-ca.crt", Contents = authority.ExportCertificatePem() },
                    new ContainerFile { Name = "security.toml", Contents = "[tls]\nmin_version = \"TLS 1.2\"\n" },
                    new ContainerFile { Name = "iam.json", Contents = IamConfiguration(), Mode = UnixFileMode.UserRead },
                ], defaultOwner: 1000, defaultGroup: 1000)
                .WithArgs("mini", "-dir=/data", "-webdav=false", "-admin.ui=false", "-bucket=" + Bucket,
                    "-s3.cert.file=" + SeaweedConfig + "/tls.crt",
                    "-s3.key.file=" + SeaweedConfig + "/tls.key",
                    "-s3.iam.config=" + SeaweedConfig + "/iam.json")
                .WithHttpsEndpoint(targetPort: 8333)
                .WaitFor(identity);

            var started = await AspireTestApplication.Start(builder, [Storage, IdentityProvider], StartupTimeout, ct);
            return new SeaweedFsWebIdentityScenario(started, authority, workloadSecret);
        }
        catch
        {
            authority.Dispose();
            throw;
        }
    }

    /// <summary>Exchanges a Keycloak workload access token with SeaweedFS STS.</summary>
    public async Task<(SessionAWSCredentials Credentials, DateTimeOffset Expiration)> AssumeRole(CancellationToken ct)
    {
        using var client = clientFactory.CreateClient();
        using var tokenForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["client_secret"] = workloadSecret,
            ["grant_type"] = "client_credentials",
        });
        var tokenEndpoint = new Uri(LocalEndpoint(IdentityProvider), RealmPath + "/protocol/openid-connect/token");
        using var tokenResponse = await client.PostAsync(tokenEndpoint, tokenForm, ct);
        tokenResponse.EnsureSuccessStatusCode();
        using var tokenDocument = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>(ct)
            ?? throw new InvalidOperationException("The identity provider returned no token document.");
        var token = tokenDocument.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("The identity provider returned no access token.");

        using var exchange = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Action"] = "AssumeRoleWithWebIdentity",
                ["Version"] = "2011-06-15",
                ["RoleArn"] = RoleArn,
                ["RoleSessionName"] = "catalog-hosted",
                ["DurationSeconds"] = "900",
                ["WebIdentityToken"] = token,
            }),
        };
        using var sts = await client.SendAsync(exchange, HttpCompletionOption.ResponseHeadersRead, ct);
        sts.EnsureSuccessStatusCode();
        await using var stream = await sts.Content.ReadAsStreamAsync(ct);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, MaxCharactersInDocument = 64 * 1024 });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, ct);
        var credentials = document.Root?.Element(StsNamespace + "AssumeRoleWithWebIdentityResult")?.Element(StsNamespace + "Credentials")
            ?? throw new InvalidOperationException("SeaweedFS STS returned no session credentials.");
        return (new SessionAWSCredentials(RequiredValue(credentials, "AccessKeyId"), RequiredValue(credentials, "SecretAccessKey"),
            RequiredValue(credentials, "SessionToken")), DateTimeOffset.Parse(RequiredValue(credentials, "Expiration"), CultureInfo.InvariantCulture));
    }

    /// <summary>Creates an S3 client that signs requests using the issued temporary session.</summary>
    public AmazonS3Client CreateS3Client(SessionAWSCredentials credentials) =>
        new(credentials, new AmazonS3Config
        {
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true,
            ServiceURL = Endpoint.GetLeftPart(UriPartial.Authority),
            HttpClientFactory = clientFactory,
        });

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await application.DisposeAsync();
        }
        finally
        {
            clientFactory.Dispose();
            authority.Dispose();
        }
    }

    private Uri LocalEndpoint(string resource)
    {
        var endpoint = application.GetEndpoint(resource, HttpsEndpoint);
        return endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttps
            ? endpoint
            : throw new InvalidOperationException("The media authentication test requires an HTTPS loopback endpoint.");
    }

    /// <summary>Waits for both HTTPS endpoints using the configured trust root.</summary>
    public async Task WaitForReady(CancellationToken ct)
    {
        using var client = clientFactory.CreateClient();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct);
        budget.CancelAfter(StartupTimeout);
        using var cadence = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        foreach (var endpoint in new[]
        {
            new Uri(LocalEndpoint(IdentityProvider), RealmPath + "/.well-known/openid-configuration"),
            new Uri(Endpoint, "/healthz"),
        })
        {
            do
            {
                try
                {
                    using var response = await client.GetAsync(endpoint, budget.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        break;
                    }
                }
                catch (HttpRequestException exception) when (exception.HttpRequestError == HttpRequestError.ConnectionError
                    || exception.GetBaseException() is SocketException { SocketErrorCode: SocketError.ConnectionReset }
                    || (exception.HttpRequestError == HttpRequestError.SecureConnectionError && exception.InnerException is IOException))
                {
                    // The container proxy can accept a connection and close it before the TLS listener starts.
                }
            } while (await cadence.WaitForNextTickAsync(budget.Token));
        }
    }

    private static string RequiredValue(XElement element, string name) =>
        element.Element(StsNamespace + name)?.Value is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"SeaweedFS STS omitted {name}.");

    private static ContainerFile[] TlsFiles(string name, X509Certificate2 authority, DateTimeOffset now)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName(name);
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        san.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.1") }, critical: true));
        using var certificate = request.Create(authority, now.AddMinutes(-5), now.AddMinutes(30), RandomNumberGenerator.GetBytes(16));
        return
        [
            new ContainerFile { Name = "tls.crt", Contents = certificate.ExportCertificatePem() },
            new ContainerFile { Name = "tls.key", Contents = key.ExportPkcs8PrivateKeyPem(), Mode = UnixFileMode.UserRead },
        ];
    }

    private static string RealmConfiguration(string workloadSecret) => JsonSerializer.Serialize(new
    {
        realm = Realm,
        enabled = true,
        clients = new[]
        {
            new
            {
                clientId = ClientId,
                enabled = true,
                protocol = "openid-connect",
                secret = workloadSecret,
                serviceAccountsEnabled = true,
                standardFlowEnabled = false,
                directAccessGrantsEnabled = false,
                protocolMappers = new[]
                {
                    new
                    {
                        name = "storage-audience", protocol = "openid-connect", protocolMapper = "oidc-audience-mapper",
                        config = new Dictionary<string, string> { ["included.client.audience"] = ClientId, ["access.token.claim"] = "true" },
                    },
                    new
                    {
                        name = "storage-role", protocol = "openid-connect", protocolMapper = "oidc-hardcoded-claim-mapper",
                        config = new Dictionary<string, string>
                        {
                            ["claim.name"] = "roles", ["claim.value"] = WriterRole, ["jsonType.label"] = "String", ["access.token.claim"] = "true",
                        },
                    },
                },
            },
        },
    });

    private static string IamConfiguration() => JsonSerializer.Serialize(new
    {
        sts = new
        {
            tokenDuration = "15m",
            maxSessionLength = "15m",
            issuer = "media-storage-sts",
            signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            providers = new[]
            {
                new
                {
                    name = IdentityProvider,
                    type = "oidc",
                    enabled = true,
                    config = new { issuer = Issuer, clientId = ClientId, jwksUri = Issuer + "/protocol/openid-connect/certs", tlsCaCert = SeaweedConfig + "/issuer-ca.crt" },
                },
            },
        },
        roles = new[]
        {
            new
            {
                roleName = WriterRole,
                roleArn = RoleArn,
                attachedPolicies = new[] { WriterRole },
                trustPolicy = new
                {
                    Version = "2012-10-17",
                    Statement = new[]
                    {
                        new
                        {
                            Effect = "Allow",
                            Principal = new { Federated = IdentityProvider },
                            Action = AssumeRoleActions,
                            Condition = new { StringEquals = new Dictionary<string, string> { ["oidc:iss"] = Issuer, ["oidc:roles"] = WriterRole } },
                        },
                    },
                },
            },
        },
        policies = new[]
        {
            new
            {
                name = WriterRole,
                document = new
                {
                    Version = "2012-10-17",
                    Statement = new[]
                    {
                        new
                        {
                            Effect = "Allow",
                            Action = MediaObjectActions,
                            Resource = new[] { $"arn:aws:s3:::{Bucket}/catalog/*" },
                        },
                    },
                },
            },
        },
    });
}
