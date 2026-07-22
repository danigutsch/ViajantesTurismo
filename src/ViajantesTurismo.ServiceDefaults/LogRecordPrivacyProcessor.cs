using OpenTelemetry;
using OpenTelemetry.Logs;

namespace ViajantesTurismo.ServiceDefaults;

internal sealed class LogRecordPrivacyProcessor : BaseProcessor<LogRecord>
{
    private const int MaximumErrorTypeLength = 256;
    private const string ExceptionTypeAttribute = "exception.type";
    private const string OriginalFormatAttribute = "{OriginalFormat}";

    public override void OnEnd(LogRecord data)
    {
        if (data is null)
        {
            return;
        }

        var attributes = data.Attributes;
        string? originalFormat = null;
        Exception? structuredException = null;
        var requiresSanitization = data.Exception is not null;

        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                if (string.Equals(attribute.Key, OriginalFormatAttribute, StringComparison.Ordinal))
                {
                    originalFormat = attribute.Value as string;
                }

                requiresSanitization |= TelemetryPrivacyAttributeClassifier.IsSensitive(attribute.Key)
                    || IsOversizedErrorType(attribute)
                    || attribute.Value is Exception;
                if (structuredException is null && attribute.Value is Exception exception)
                {
                    structuredException = exception;
                }
            }
        }

        data.Body = originalFormat;
        data.FormattedMessage = null;

        if (!requiresSanitization)
        {
            return;
        }

        var exceptionType = GetExceptionType(data.Exception ?? structuredException);
        data.Exception = null;
        var capacity = (attributes?.Count ?? 0) + (exceptionType is null ? 0 : 1);
        var sanitizedAttributes = new List<KeyValuePair<string, object?>>(capacity);

        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                if (TelemetryPrivacyAttributeClassifier.IsSensitive(attribute.Key)
                    || attribute.Value is Exception
                    || (exceptionType is not null && string.Equals(attribute.Key, ExceptionTypeAttribute, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                sanitizedAttributes.Add(LimitErrorType(attribute));
            }
        }

        if (exceptionType is not null)
        {
            sanitizedAttributes.Add(new KeyValuePair<string, object?>(ExceptionTypeAttribute, exceptionType));
        }

        data.Attributes = sanitizedAttributes;
    }

    private static string? GetExceptionType(Exception? exception)
    {
        if (exception is null)
        {
            return null;
        }

        var value = exception.GetType().FullName ?? exception.GetType().Name;
        return value.Length <= MaximumErrorTypeLength ? value : value[..MaximumErrorTypeLength];
    }

    private static bool IsOversizedErrorType(KeyValuePair<string, object?> attribute)
    {
        return IsErrorType(attribute.Key)
            && attribute.Value is string value
            && value.Length > MaximumErrorTypeLength;
    }

    private static KeyValuePair<string, object?> LimitErrorType(KeyValuePair<string, object?> attribute)
    {
        if (!IsOversizedErrorType(attribute))
        {
            return attribute;
        }

        return attribute.Value is string value
            ? new KeyValuePair<string, object?>(attribute.Key, value[..MaximumErrorTypeLength])
            : attribute;
    }

    private static bool IsErrorType(string name)
    {
        return string.Equals(name, "error.type", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ExceptionTypeAttribute, StringComparison.OrdinalIgnoreCase);
    }

}
