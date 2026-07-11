using System.Diagnostics;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class SeaweedFsS3SensitiveAttributeRedactionProcessorTests
{
    [Fact]
    public void OnEnd_removes_sensitive_attributes_from_s3_activity()
    {
        // Arrange
        using var activity = new Activity("S3.PutObject");
        activity.Start();
        activity.SetTag("aws.s3.bucket", "media");
        activity.SetTag("aws.s3.key", "media/customer/image.jpg");
        activity.SetTag("aws.s3.copy_source", "private/source.jpg");
        activity.SetTag("aws.s3.delete", "private/delete.jpg");
        activity.SetTag("aws.s3.upload_id", "upload-id");
        activity.SetTag("aws.s3.part_number", 1);
        activity.SetTag("url.full", "https://seaweedfs/media/private/image.jpg");
        activity.SetTag("http.url", "https://seaweedfs/media/private/image.jpg");
        using var processor = new SeaweedFsS3SensitiveAttributeRedactionProcessor();

        // Act
        processor.OnEnd(activity);

        // Assert
        activity.GetTagItem("aws.s3.bucket").ShouldBe("media");
        activity.GetTagItem("aws.s3.key").ShouldBeNull();
        activity.GetTagItem("aws.s3.copy_source").ShouldBeNull();
        activity.GetTagItem("aws.s3.delete").ShouldBeNull();
        activity.GetTagItem("aws.s3.upload_id").ShouldBeNull();
        activity.GetTagItem("aws.s3.part_number").ShouldBeNull();
        activity.GetTagItem("url.full").ShouldBeNull();
        activity.GetTagItem("http.url").ShouldBeNull();
    }

    [Fact]
    public void OnEnd_leaves_non_s3_activity_attributes_unchanged()
    {
        // Arrange
        using var activity = new Activity("HTTP GET");
        activity.Start();
        activity.SetTag("url.full", "https://example.test/public");
        using var processor = new SeaweedFsS3SensitiveAttributeRedactionProcessor();

        // Act
        processor.OnEnd(activity);

        // Assert
        activity.GetTagItem("url.full").ShouldBe("https://example.test/public");
    }
}
