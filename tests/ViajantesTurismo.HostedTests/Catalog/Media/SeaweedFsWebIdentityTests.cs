using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using SharedKernel.Testing;
using ViajantesTurismo.HostedTests.Catalog.Media.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Media;

/// <summary>Exercises Media S3 transport and workload identity against the pinned provider images.</summary>
[Collection(SeaweedFsWebIdentityGroup.Name)]
[Trait(TestTraitNames.FocusName, TestTraitValues.SecurityCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.MediaInfrastructureArea)]
[TestScope(TestScope.Integration)]
public sealed class SeaweedFsWebIdentityTests
{
    [Fact]
    public async Task Exchanges_a_workload_token_for_temporary_object_storage_credentials()
    {
        // Arrange
        const string objectKey = "catalog/session-proof.txt";
        const string objectContent = "Hosted media storage session";
        var ct = TestContext.Current.CancellationToken;
        var timeProvider = TimeProvider.System;
        await using var scenario = await SeaweedFsWebIdentityScenario.Start(timeProvider, ct);
        await scenario.WaitForReady(ct);
        using var untrustedHandler = new SocketsHttpHandler { UseProxy = false };
        using var untrustedClient = new HttpClient(untrustedHandler, disposeHandler: false);
        using var unsignedClient = scenario.CreateHttpClient();
        using var unsignedContent = new StringContent(objectContent);
        var objectEndpoint = new Uri(scenario.Endpoint, $"/{SeaweedFsWebIdentityScenario.Bucket}/{objectKey}");
        var started = timeProvider.GetUtcNow();

        // Act
        Func<Task> connectWithoutCa = async () =>
        {
            using var response = await untrustedClient.GetAsync(new Uri(scenario.Endpoint, "/healthz"), ct);
        };
        var rejectedTls = await connectWithoutCa.ShouldThrow<HttpRequestException>();
        using var rejectedUnsignedWrite = await unsignedClient.PutAsync(objectEndpoint, unsignedContent, ct);
        var (credentials, expiration) = await scenario.AssumeRole(ct);
        var latestExpiration = timeProvider.GetUtcNow().AddMinutes(15);
        using var storage = scenario.CreateS3Client(credentials);
        var put = await storage.PutObjectAsync(new PutObjectRequest
        {
            BucketName = SeaweedFsWebIdentityScenario.Bucket,
            Key = objectKey,
            ContentBody = objectContent,
        }, ct);
        using var stored = await storage.GetObjectAsync(SeaweedFsWebIdentityScenario.Bucket, objectKey, ct);
        using var content = new StreamReader(stored.ResponseStream, leaveOpen: true);
        var received = await content.ReadToEndAsync(ct);
        Func<Task> putOutsidePrefix = () => storage.PutObjectAsync(new PutObjectRequest
        {
            BucketName = SeaweedFsWebIdentityScenario.Bucket,
            Key = "other/session-proof.txt",
            ContentBody = objectContent,
        }, ct);
        var rejectedWrite = await putOutsidePrefix.ShouldThrow<AmazonS3Exception>();
        var delete = await storage.DeleteObjectAsync(SeaweedFsWebIdentityScenario.Bucket, objectKey, ct);

        // Assert
        rejectedTls.HttpRequestError.ShouldBe(HttpRequestError.SecureConnectionError);
        rejectedUnsignedWrite.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        expiration.ShouldBeGreaterThan(started);
        expiration.ShouldBeLessThanOrEqualTo(latestExpiration);
        put.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
        received.ShouldBe(objectContent);
        rejectedWrite.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        delete.HttpStatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
