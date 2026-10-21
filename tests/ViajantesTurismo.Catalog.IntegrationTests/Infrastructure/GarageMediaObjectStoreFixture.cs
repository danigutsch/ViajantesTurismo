using System.Security.Cryptography;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PandaBear.IntegrationTesting;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

public sealed class GarageMediaObjectStoreFixture : IAsyncLifetime
{
    private const string ResourceName = "garage";
    private const string BucketName = "media-evaluation";
    private AspireTestApplication? app;
    private AmazonS3Client? client;
    private string? configurationDirectory;
    private IMediaObjectStore? store;

    public IMediaObjectStore Store => store ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri Endpoint => app?.GetEndpoint(ResourceName, "s3") ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Task SeedNonChunkedObject(string objectKey, string content, CancellationToken ct)
    {
        var initializedClient = client ?? throw new InvalidOperationException("Fixture is not initialized.");
        return initializedClient.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = objectKey,
            ContentBody = content,
            ContentType = "text/plain",
            UseChunkEncoding = false,
            Metadata = { ["checksum"] = "test-checksum" }
        }, ct);
    }

    public async ValueTask InitializeAsync()
    {
        configurationDirectory = Path.Combine(Path.GetTempPath(), $"garage-evaluation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(configurationDirectory);
        var configurationPath = Path.Combine(configurationDirectory, "garage.toml");
        await File.WriteAllTextAsync(configurationPath, $$"""
            metadata_dir = "/tmp/meta"
            data_dir = "/tmp/data"
            db_engine = "sqlite"
            replication_factor = 1
            rpc_bind_addr = "[::]:3901"
            rpc_public_addr = "127.0.0.1:3901"
            rpc_secret = "{{Convert.ToHexString(RandomNumberGenerator.GetBytes(32))}}"
            [s3_api]
            s3_region = "garage"
            api_bind_addr = "[::]:3900"
            root_domain = ".s3.garage.localhost"
            """, TestContext.Current.CancellationToken);
        var accessKey = $"GK{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";
        var secretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var builder = AspireTestApplication.CreateBuilder($"--DcpPublisher:ResourceNameSuffix=garage-{Guid.NewGuid():N}");
        builder.AddContainer(ResourceName, "dxflrs/garage", "v2.4.1@sha256:0d7c74fc8ca6fef68a5a941c0e7558c8b1e92ba3588fa7505400e1350456c796")
            .WithImageRegistry("docker.io")
            .WithBindMount(configurationPath, "/etc/garage.toml", isReadOnly: true)
            .WithEnvironment("GARAGE_DEFAULT_ACCESS_KEY", accessKey)
            .WithEnvironment("GARAGE_DEFAULT_SECRET_KEY", secretKey)
            .WithEnvironment("GARAGE_DEFAULT_BUCKET", BucketName)
            .WithArgs("/garage", "server", "--single-node", "--default-bucket")
            .WithHttpEndpoint(targetPort: 3900, name: "s3");
        app = await AspireTestApplication.Start(builder, [ResourceName], TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
        client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), new AmazonS3Config
        {
            AuthenticationRegion = "garage",
            ForcePathStyle = true,
            ServiceURL = Endpoint.GetLeftPart(UriPartial.Authority)
        });
        store = new SeaweedFsMediaObjectStore(client, Options.Create(new SeaweedFsMediaObjectStorageOptions
        {
            Endpoint = Endpoint,
            Bucket = BucketName,
            AccessKey = accessKey,
            SecretKey = secretKey,
            AutoProvisionBucket = false,
            PublicBaseUri = new Uri("https://cdn.example.test/media/")
        }));
    }

    public async ValueTask DisposeAsync()
    {
        client?.Dispose();
        if (app is not null)
        {
            await app.DisposeAsync();
        }

        if (configurationDirectory is not null)
        {
            Directory.Delete(configurationDirectory, recursive: true);
        }
    }
}
