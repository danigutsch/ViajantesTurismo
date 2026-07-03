using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Management.WebTests;

internal sealed record CollectedLogEntry(
    LogLevel LogLevel,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, string> State);
