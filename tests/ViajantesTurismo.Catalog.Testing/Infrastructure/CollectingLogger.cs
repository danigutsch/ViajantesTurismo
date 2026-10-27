using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Logger that records formatted messages, structured values and exceptions for assertions.</summary>
internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly List<string> messages = [];
    private readonly List<KeyValuePair<string, object?>> structuredValues = [];
    private readonly List<Exception> exceptions = [];

    /// <summary>Formatted messages in the order logged.</summary>
    public IReadOnlyList<string> Messages => messages;

    /// <summary>Key-value pairs from structured log states, in the order logged.</summary>
    public IReadOnlyList<KeyValuePair<string, object?>> StructuredValues => structuredValues;

    /// <summary>Exceptions passed to the logger, in the order logged.</summary>
    public IReadOnlyList<Exception> Exceptions => exceptions;

    /// <summary>Scopes are not tracked; always returns null.</summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>Always enabled, for every log level.</summary>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>Records the formatted message, any exception and any structured values.</summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        messages.Add(formatter(state, exception));
        if (exception is not null)
        {
            exceptions.Add(exception);
        }

        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            structuredValues.AddRange(values);
        }
    }
}
