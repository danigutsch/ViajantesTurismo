using System.Diagnostics;
using OpenTelemetry;

namespace ViajantesTurismo.ServiceDefaults;

internal sealed class ActivityPrivacyProcessor : BaseProcessor<Activity>
{
    private const int MaximumErrorTypeLength = 256;

    public override void OnEnd(Activity data)
    {
        if (data is null)
        {
            return;
        }

        var sensitiveAttributeNames = data.TagObjects
            .Where(attribute => TelemetryPrivacyAttributeClassifier.IsSensitive(attribute.Key)
                || attribute.Value is Exception)
            .Select(static attribute => attribute.Key)
            .ToArray();

        foreach (var attributeName in sensitiveAttributeNames)
        {
            data.SetTag(attributeName, null);
        }

        if (data.StatusDescription is not null)
        {
            data.SetStatus(data.Status);
        }

        LimitErrorType(data, "error.type");
        LimitErrorType(data, "exception.type");
    }

    private static void LimitErrorType(Activity activity, string attributeName)
    {
        (string Name, string Value)? limitedAttribute = null;

        foreach (var attribute in activity.TagObjects)
        {
            if (string.Equals(attribute.Key, attributeName, StringComparison.OrdinalIgnoreCase)
                && attribute.Value is string value
                && value.Length > MaximumErrorTypeLength)
            {
                limitedAttribute = (attribute.Key, value[..MaximumErrorTypeLength]);
                break;
            }
        }

        if (limitedAttribute is { } attributeToUpdate)
        {
            activity.SetTag(attributeToUpdate.Name, attributeToUpdate.Value);
        }
    }
}
