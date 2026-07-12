using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class SeaweedFsS3SensitiveAttributeRedactionProcessor : BaseProcessor<Activity>
{
    private static string[] SensitiveAttributes { get; } =
    [
        "aws.s3.key",
        "aws.s3.copy_source",
        "aws.s3.delete",
        "aws.s3.upload_id",
        "aws.s3.part_number",
        "url.full",
        "http.url"
    ];

    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.GetTagItem("aws.s3.bucket") is null)
        {
            return;
        }

        foreach (var attribute in SensitiveAttributes)
        {
            data.SetTag(attribute, null);
        }
    }
}
