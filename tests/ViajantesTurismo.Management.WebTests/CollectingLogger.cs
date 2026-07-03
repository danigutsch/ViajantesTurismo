using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly List<CollectedLogEntry> _entries = [];

    public IReadOnlyList<CollectedLogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

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
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            result[value.Key] = value.Value?.ToString() ?? string.Empty;
        }

        return result;
    }
}
