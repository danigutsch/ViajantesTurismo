namespace ViajantesTurismo.ServiceDefaults.Tests;

internal sealed record CapturedLogRecord(
    string? CategoryName,
    string? Body,
    string? FormattedMessage,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> Attributes,
    IReadOnlyList<object?> Scopes);
