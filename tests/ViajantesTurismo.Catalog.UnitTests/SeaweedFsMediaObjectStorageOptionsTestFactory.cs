using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class SeaweedFsMediaObjectStorageOptionsTestFactory
{
    public static SeaweedFsMediaObjectStorageOptions CreateValidOptions()
    {
        return new SeaweedFsMediaObjectStorageOptions
        {
            Endpoint = new Uri("https://seaweedfs.example"),
            PublicBaseUri = new Uri("https://cdn.example/media/"),
            Bucket = "media",
            AccessKey = "access",
            SecretKey = "secret",
            BucketProvisioningTimeout = TimeSpan.FromSeconds(30)
        };
    }
}
