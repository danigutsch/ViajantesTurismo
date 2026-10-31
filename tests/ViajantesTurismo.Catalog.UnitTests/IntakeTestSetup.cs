using PandaBear.ObjectStorage;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Builds intakes and upload requests for the hardening tests.</summary>
internal static class IntakeTestSetup
{
    /// <summary>Creates an intake with a passing scanner unless one is supplied.</summary>
    internal static MediaImageUploadIntake CreateIntake(
        IObjectStore objectStore,
        IPublicMediaImageStore imageStore,
        StubMediaUploadScanner? scanner = null) =>
        MediaImageUploadIntakeTestFactory.Create(scanner ?? new StubMediaUploadScanner(MediaUploadScanResult.Passed), objectStore, imageStore);

    /// <summary>Creates a request over the content with one tour link.</summary>
    internal static MediaImageUploadIntakeRequest CreateRequest(byte[] content, string fileName, string contentType, Guid? mediaImageId = null) =>
        new(
            mediaImageId ?? Guid.CreateVersion7(),
            new MemoryStream(content),
            fileName,
            contentType,
            content.Length,
            "Cyclists in the mountains",
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
}
