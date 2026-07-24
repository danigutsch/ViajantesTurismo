using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

internal sealed class DocumentPrivacyLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> messages = new();

    public IReadOnlyCollection<string> Messages => messages.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
            if (state is IEnumerable<KeyValuePair<string, object?>> structuredValues)
            {
                foreach (var value in structuredValues)
                {
                    messages.Enqueue(string.Concat(value.Key, "=", value.Value));
                }
            }

            if (exception is not null)
            {
                messages.Enqueue(exception.ToString());
            }
        }
    }

    private sealed class NoopScope : IDisposable
    {
        public static NoopScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
