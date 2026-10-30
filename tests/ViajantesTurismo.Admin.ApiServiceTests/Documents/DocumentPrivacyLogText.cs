using PandaBear.Testing.Logging;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

/// <summary>Flattens recorded log output into one searchable text for privacy assertions.</summary>
internal static class DocumentPrivacyLogText
{
    /// <summary>Joins every record's message, structured key=value pairs and exception text with newlines.</summary>
    public static string From(RecordingLoggerProvider provider) =>
        string.Join(
            '\n',
            provider.Records.SelectMany(record => record.State
                .Select(value => string.Concat(value.Key, "=", value.Value))
                .Prepend(record.Message)
                .Append(record.Exception?.ToString() ?? string.Empty)));
}
