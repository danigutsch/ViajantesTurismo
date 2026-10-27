using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Logger that captures formatted messages, including exception text, for test assertions.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<string> entries = [];

    /// <summary>Formatted log messages in the order they were written.</summary>
    public IReadOnlyList<string> Entries => entries;

    /// <summary>Ignores scopes and returns <see langword="null"/>.</summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <summary>Always reports every log level as enabled.</summary>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>Appends the formatted message followed by the exception text to <see cref="Entries"/>.</summary>
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
