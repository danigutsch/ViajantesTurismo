using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly List<string> messages = [];
    private readonly List<KeyValuePair<string, object?>> structuredValues = [];
    private readonly List<Exception> exceptions = [];

    public IReadOnlyList<string> Messages => messages;

    public IReadOnlyList<KeyValuePair<string, object?>> StructuredValues => structuredValues;

    public IReadOnlyList<Exception> Exceptions => exceptions;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

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
