using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

/// <summary>
/// Logger that records every log call as a <see cref="CollectedLogEntry" /> for assertions.
/// </summary>
internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly List<CollectedLogEntry> _entries = [];

    /// <summary>
    /// Gets the log entries recorded so far, in call order.
    /// </summary>
    public IReadOnlyList<CollectedLogEntry> Entries => _entries;

    /// <summary>
    /// Ignores the scope and returns null.
    /// </summary>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    /// Reports every log level as enabled.
    /// </summary>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    /// Records the formatted message and structured state of the call.
    /// </summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new CollectedLogEntry(logLevel, eventId, formatter(state, exception), ReadState(state)));
    }

    private static Dictionary<string, string> ReadState<TState>(TState state)
    {
        if (state is not IEnumerable<KeyValuePair<string, object?>> values)
        {
            return [];
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            result[value.Key] = value.Value?.ToString() ?? string.Empty;
        }

        return result;
    }
}
