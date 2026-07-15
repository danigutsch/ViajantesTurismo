using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Management.WebTests;

internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<string> entries = [];

    public IReadOnlyList<string> Entries => entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
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
        entries.Add(string.Concat(formatter(state, exception), exception));
    }
}
