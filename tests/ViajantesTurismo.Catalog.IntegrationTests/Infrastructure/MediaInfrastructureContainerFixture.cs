using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using SharedKernel.IntegrationTesting;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

public sealed class MediaInfrastructureContainerFixture : IAsyncLifetime
{
    private const string ClamAvResourceName = "clamav";
    private const string SeaweedFsResourceName = "seaweedfs";
    private AspireTestApplication? _app;
    private string? _accessKey;
    private string? _bucket;
    private string? _secretKey;

    public Uri ClamAvEndpoint => GetApp().GetEndpoint(ClamAvResourceName, ClamAvResource.TcpEndpointName);

    public Uri SeaweedFsS3Endpoint => GetApp().GetEndpoint(SeaweedFsResourceName, SeaweedFsResource.S3EndpointName);

    public string Bucket => _bucket ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        var accessKey = CreateCredential(24);
        var secretKey = CreateCredential(32);
        var bucket = $"media-{Guid.NewGuid():N}";
        var resourceNameSuffix = $"media-{Guid.NewGuid():N}";
        var builder = AspireTestApplication.CreateBuilder(
            $"--Parameters:{SeaweedFsResourceName}-access-key={accessKey}",
            $"--Parameters:{SeaweedFsResourceName}-secret-key={secretKey}",
            $"--Parameters:{SeaweedFsResourceName}-bucket={bucket}",
            $"--DcpPublisher:ResourceNameSuffix={resourceNameSuffix}");
        _ = builder.AddClamAv(ClamAvResourceName);
        _ = builder.AddSeaweedFs(SeaweedFsResourceName, bucket);

        _app = await AspireTestApplication.Start(
            builder,
            [ClamAvResourceName, SeaweedFsResourceName],
            TimeSpan.FromMinutes(3),
            TestContext.Current.CancellationToken);
        _accessKey = accessKey;
        _bucket = bucket;
        _secretKey = secretKey;
    }

    public async ValueTask DisposeAsync()
    {
        var app = _app;
        _app = null;
        _accessKey = null;
        _bucket = null;
        _secretKey = null;

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    public AmazonS3Client CreateSeaweedFsClient()
    {
        var accessKey = _accessKey ?? throw new InvalidOperationException("Fixture is not initialized.");
        var secretKey = _secretKey ?? throw new InvalidOperationException("Fixture is not initialized.");
        var configuration = new AmazonS3Config
        {
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true,
            ServiceURL = SeaweedFsS3Endpoint.GetLeftPart(UriPartial.Authority)
        };

        return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), configuration);
    }

    private static string CreateCredential(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        try
        {
            return Convert.ToHexString(bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private AspireTestApplication GetApp() => _app ?? throw new InvalidOperationException("Fixture is not initialized.");
}
