using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class SeaweedFsS3SensitiveAttributeRedactionProcessor : BaseProcessor<Activity>
{
    private static string[] SensitiveAttributes { get; } =
    [
        "aws.s3.bucket",
        "aws.s3.bucket.name",
        "aws.s3.key",
        "aws.s3.copy_source",
        "aws.s3.delete",
        "aws.s3.upload_id",
        "aws.s3.part_number",
        "aws.request_id",
        "aws.extended_request_id",
        "url.full",
        "http.url"
    ];

    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!data.TagObjects.Any(static attribute =>
                attribute.Key.StartsWith("aws.", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var attribute in SensitiveAttributes)
        {
            data.SetTag(attribute, null);
        }
    }
}
