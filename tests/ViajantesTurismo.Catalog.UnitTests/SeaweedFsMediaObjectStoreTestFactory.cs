using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class SeaweedFsMediaObjectStoreTestFactory
{
    public static SeaweedFsMediaObjectStore CreateStore(FakeAmazonS3Client client, bool autoProvisionBucket = true)
    {
        return new SeaweedFsMediaObjectStore(client, Options.Create(new SeaweedFsMediaObjectStorageOptions
        {
            Endpoint = new Uri("http://seaweedfs:8333/"),
            PublicBaseUri = new Uri("/cdn/", UriKind.Relative),
            Bucket = "media",
            AccessKey = "access",
            SecretKey = "secret",
            AutoProvisionBucket = autoProvisionBucket
        }));
    }
}
