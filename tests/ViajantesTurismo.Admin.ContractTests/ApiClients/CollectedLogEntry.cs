using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

/// <summary>
/// Captured log call recorded by <see cref="CollectingLogger{T}" />.
/// </summary>
/// <param name="LogLevel">Severity of the log call.</param>
/// <param name="EventId">Event identifier of the log call.</param>
/// <param name="Message">Formatted message text.</param>
/// <param name="State">Structured state values rendered as strings.</param>
internal sealed record CollectedLogEntry(
    LogLevel LogLevel,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, string> State);
