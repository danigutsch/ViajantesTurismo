using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

public sealed class MediaInfrastructureContainerFixture(HostedTestFixture shared) : IAsyncLifetime
{
    private string? bucket;
    private Uri? clamAvEndpoint;
    private Uri? seaweedFsS3Endpoint;

    public Uri ClamAvEndpoint => clamAvEndpoint ?? throw new InvalidOperationException("Fixture is not initialized.");
    public Uri SeaweedFsS3Endpoint => seaweedFsS3Endpoint ?? throw new InvalidOperationException("Fixture is not initialized.");
    public string Bucket => bucket ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var scanner = shared.GetClamAvEndpoint(ct);
        var storage = shared.GetSeaweedFsEndpoint(ct);
        await Task.WhenAll(scanner, storage);
        clamAvEndpoint = await scanner;
        seaweedFsS3Endpoint = await storage;
        bucket = $"media-{Guid.NewGuid():N}";
        using var client = CreateSeaweedFsClient();
        await client.PutBucketAsync(bucket, TestContext.Current.CancellationToken);
    }

    public AmazonS3Client CreateSeaweedFsClient()
    {
        return new AmazonS3Client(new BasicAWSCredentials(shared.SeaweedAccessKey, shared.SeaweedSecretKey), new AmazonS3Config
        {
            AuthenticationRegion = "us-east-1",
            ForcePathStyle = true,
            ServiceURL = SeaweedFsS3Endpoint.GetLeftPart(UriPartial.Authority),
        });
    }

    public async ValueTask DisposeAsync()
    {
        var ownedBucket = bucket;
        bucket = null;
        if (ownedBucket is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var client = CreateSeaweedFsClient();
            ListObjectsV2Response page;
            do
            {
                page = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = ownedBucket }, timeout.Token);
                foreach (var item in page.S3Objects ?? [])
                {
                    await client.DeleteObjectAsync(ownedBucket, item.Key, timeout.Token);
                }
            } while (page.IsTruncated is true);

            await client.DeleteBucketAsync(ownedBucket, timeout.Token);
        }
    }
}
