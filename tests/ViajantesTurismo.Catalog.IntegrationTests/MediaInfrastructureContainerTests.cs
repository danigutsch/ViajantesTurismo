using System.Net;
using System.Net.Sockets;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SharedKernel.MalwareScanning;
using SharedKernel.MalwareScanning.ClamAv.Testing;
using SharedKernel.Results;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Catalog.IntegrationTests;

[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.MediaInfrastructureArea)]
public sealed class MediaInfrastructureContainerTests(MediaInfrastructureContainerFixture fixture)
    : IClassFixture<MediaInfrastructureContainerFixture>
{
    [Fact]
    public async Task Scan_returns_passed_for_clean_content()
    {
        // Arrange
        using var scannerScope = ClamAvMalwareScannerTestScope.Create(new ClamAvMalwareScannerTestSettings(fixture.ClamAvEndpoint));
        var content = "clean content"u8.ToArray();

        // Act
        var result = await scannerScope.Scanner.Scan(new MemoryStream(content), content.Length, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MalwareScanStatus.Passed);
    }

    [Fact]
    public async Task Scan_rejects_the_eicar_test_string()
    {
        // Arrange
        using var scannerScope = ClamAvMalwareScannerTestScope.Create(new ClamAvMalwareScannerTestSettings(fixture.ClamAvEndpoint));
        var content = Convert.FromBase64String("WDVPIVAlQEFQWzRcUFpYNTQoUF4pN0NDKTd9JEVJQ0FSLVNUQU5EQVJELUFOVElWSVJVUy1URVNULUZJTEUhJEgrSCo=");

        // Act
        var result = await scannerScope.Scanner.Scan(new MemoryStream(content), content.Length, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(MalwareScanStatus.Rejected);
    }

    [Fact]
    public async Task Scanner_outage_does_not_write_to_storage()
    {
        // Arrange
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var unavailablePort = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var unavailableEndpoint = new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), unavailablePort).Uri;
        using var scannerScope = ClamAvMalwareScannerTestScope.Create(new ClamAvMalwareScannerTestSettings(unavailableEndpoint, TimeSpan.FromMilliseconds(100)));
        var objectStore = new RecordingMediaObjectStore();
        var validationOptions = new MediaUploadValidationOptions();
        var intake = new MediaImageUploadIntake(
            new MediaUploadValidator(validationOptions),
            new MalwareScannerMediaUploadScanner(scannerScope.Scanner),
            objectStore,
            new UnexpectedPublicMediaImageStore(),
            new UnexpectedIntegrationEventOutbox(),
            Options.Create(validationOptions));
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        var request = new MediaImageUploadIntakeRequest(
            Guid.CreateVersion7(),
            new MemoryStream(content),
            "upload.jpg",
            "image/jpeg",
            content.Length,
            "Test image",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);

        // Act
        var result = await intake.Accept(request, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Unavailable);
        objectStore.PutCount.ShouldBe(0);
    }

    [Fact]
    public async Task SeaweedFs_denies_an_unsigned_put()
    {
        // Arrange
        using var client = new HttpClient { BaseAddress = fixture.SeaweedFsS3Endpoint };
        using var content = new ByteArrayContent("unsigned"u8.ToArray());

        // Act
        using var response = await client.PutAsync(
            new Uri($"{fixture.Bucket}/unsigned-object", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SeaweedFs_performs_crud_with_generated_configured_credentials()
    {
        // Arrange
        using var client = fixture.CreateSeaweedFsClient();
        var bucket = $"credentials-{Guid.NewGuid():N}";
        var objectName = $"object-{Guid.NewGuid():N}.txt";
        await client.PutBucketAsync(bucket, TestContext.Current.CancellationToken);

        // Act
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = objectName,
            ContentBody = "first"
        }, TestContext.Current.CancellationToken);
        using var firstResponse = await client.GetObjectAsync(bucket, objectName, TestContext.Current.CancellationToken);
        using var firstReader = new StreamReader(firstResponse.ResponseStream, Encoding.UTF8, leaveOpen: false);
        var firstContent = await firstReader.ReadToEndAsync(TestContext.Current.CancellationToken);
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = objectName,
            ContentBody = "second"
        }, TestContext.Current.CancellationToken);
        using var secondResponse = await client.GetObjectAsync(bucket, objectName, TestContext.Current.CancellationToken);
        using var secondReader = new StreamReader(secondResponse.ResponseStream, Encoding.UTF8, leaveOpen: false);
        var secondContent = await secondReader.ReadToEndAsync(TestContext.Current.CancellationToken);
        await client.DeleteObjectAsync(bucket, objectName, TestContext.Current.CancellationToken);
        Func<Task> getDeletedObjectMetadata = async () =>
            _ = await client.GetObjectMetadataAsync(bucket, objectName, TestContext.Current.CancellationToken);

        // Assert
        firstContent.ShouldBe("first");
        secondContent.ShouldBe("second");
        var exception = await getDeletedObjectMetadata.ShouldThrow<AmazonS3Exception>();
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
