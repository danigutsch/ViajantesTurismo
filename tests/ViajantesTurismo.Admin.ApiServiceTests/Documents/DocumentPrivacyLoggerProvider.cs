using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

/// <summary>Logger provider that captures messages, structured values and exceptions for privacy assertions.</summary>
internal sealed class DocumentPrivacyLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> messages = new();

    /// <summary>Gets a snapshot of every captured log text.</summary>
    public IReadOnlyCollection<string> Messages => [.. messages];

    /// <summary>Creates a logger that writes into the shared message queue.</summary>
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

    /// <summary>Releases nothing; the provider owns no disposable resources.</summary>
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
