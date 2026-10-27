namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Snapshot of an exported OpenTelemetry log record for test assertions.</summary>
/// <param name="CategoryName">Logger category name.</param>
/// <param name="Body">Raw log message template.</param>
/// <param name="FormattedMessage">Rendered log message.</param>
/// <param name="Exception">Logged exception, if any.</param>
/// <param name="Attributes">Structured log attributes.</param>
/// <param name="Scopes">Log scopes active when the record was written.</param>
internal sealed record CapturedLogRecord(
    string? CategoryName,
    string? Body,
    string? FormattedMessage,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Attributes,
    IReadOnlyList<object?> Scopes);
