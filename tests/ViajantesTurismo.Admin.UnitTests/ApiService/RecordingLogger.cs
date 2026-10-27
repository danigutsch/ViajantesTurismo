using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Logger that records level, event id, formatted message, structured values and exception for assertions.</summary>
internal sealed class RecordingLogger : ILogger
{
    private readonly List<RecordedLogEntry> entries = [];

    /// <summary>Entries in the order logged.</summary>
    public IReadOnlyList<RecordedLogEntry> Entries => entries;

    /// <summary>Scopes are not tracked; always returns null.</summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>Always enabled, for every log level.</summary>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>Records the entry.</summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var values = state is IEnumerable<KeyValuePair<string, object?>> pairs ? pairs.ToArray() : [];
        entries.Add(new RecordedLogEntry
        {
            Level = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Values = values,
            Exception = exception
        });
    }
}
