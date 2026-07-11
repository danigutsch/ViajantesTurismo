using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

internal sealed record CollectedLogEntry(
    LogLevel LogLevel,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, string> State);
