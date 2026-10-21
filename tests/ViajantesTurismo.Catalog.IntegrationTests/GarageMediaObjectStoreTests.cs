using System.Net;
using System.Net.Http.Headers;
using Amazon.S3.Model;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Catalog.IntegrationTests;

[Trait(TestTraitNames.AreaName, TestTraits.MediaInfrastructureArea)]
public sealed class GarageMediaObjectStoreTests(GarageMediaObjectStoreFixture fixture) : IClassFixture<GarageMediaObjectStoreFixture>
{
    [Fact]
    public async Task Reads_lists_and_deletes_non_chunked_objects_against_garage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var objectKey = $"evaluation/{Guid.NewGuid():N}/image.txt";
        var store = fixture.Store;
        await fixture.SeedNonChunkedObject(objectKey, "garage-evaluation", ct);

        // Act
        var exists = await store.Exists(objectKey, ct);
        using var read = await store.OpenRead(objectKey, ct);
        using var reader = new StreamReader(read.Content);
        var content = await reader.ReadToEndAsync(ct);
        var keys = await store.ListKeys(objectKey, ct);
        var inventory = await store.ListObjects(objectKey, ct);
        await store.Delete(objectKey, ct);
        await store.Delete(objectKey, ct);
        var existsAfterDelete = await store.Exists(objectKey, ct);

        // Assert
        exists.ShouldBeTrue();
        content.ShouldBe("garage-evaluation");
        read.Checksum.ShouldBe("test-checksum");
        read.ContentType.ShouldBe("text/plain");
        read.Length.ShouldBe(17);
        keys.ShouldContain(objectKey);
        inventory.Count.ShouldBe(1);
        inventory[0].ObjectKey.ShouldBe(objectKey);
        inventory[0].LastModifiedAt.ShouldNotBe(default);
        store.GetPublicUri(objectKey).ShouldBe(new Uri($"https://cdn.example.test/media/{objectKey}"));
        existsAfterDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task Identifies_current_adapter_upload_incompatibility_without_disabling_payload_integrity()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        using var source = new MemoryStream("garage-evaluation"u8.ToArray());
        var request = new MediaObjectWriteRequest($"evaluation/{Guid.NewGuid():N}/unsupported.txt", source, "text/plain", source.Length);

        // Act
        Func<Task> put = async () => await fixture.Store.Put(request, ct);

        // Assert
        var error = await put.ShouldThrow<InvalidRequestException>();
        error.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        error.Message.ShouldContain("Invalid payload signature", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_unsigned_writes()
    {
        // Arrange
        using var client = new HttpClient { BaseAddress = fixture.Endpoint };
        using var content = new ByteArrayContent("unsigned"u8.ToArray());

        // Act
        using var response = await client.PutAsync(new Uri("media-evaluation/unsigned", UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Enforces_presigned_content_type_and_length()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var key = $"evaluation/{Guid.NewGuid():N}/direct.txt";
        var ticket = await fixture.Store.CreateUploadUrl(new MediaObjectUploadRequest(key, "text/plain", 4, TimeSpan.FromMinutes(1)), ct);
        using var client = new HttpClient();
        using var validContent = new ByteArrayContent("safe"u8.ToArray());
        validContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var invalidContent = new ByteArrayContent("longer"u8.ToArray());
        invalidContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        using var wrongType = new ByteArrayContent("safe"u8.ToArray());
        wrongType.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // Act
        using var accepted = await client.PutAsync(ticket.UploadUri, validContent, ct);
        using var rejectedLength = await client.PutAsync(ticket.UploadUri, invalidContent, ct);
        using var rejectedType = await client.PutAsync(ticket.UploadUri, wrongType, ct);
        await fixture.Store.Delete(key, ct);

        // Assert
        accepted.IsSuccessStatusCode.ShouldBeTrue();
        rejectedLength.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        rejectedType.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
