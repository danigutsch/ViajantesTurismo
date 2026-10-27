using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.ServiceDefaults.Tests;

/// <summary>Test logger provider that collects formatted messages and exception text.</summary>
/// <param name="messages">Queue receiving each logged message.</param>
internal sealed class CollectingLoggerProvider(ConcurrentQueue<string> messages) : ILoggerProvider
{
    /// <summary>Creates a logger that writes to the shared message queue.</summary>
    public ILogger CreateLogger(string categoryName)
    {
        return new CollectingLogger(messages);
    }

    /// <summary>Releases nothing; the provider owns no resources.</summary>
    public void Dispose()
    {
    }

    private sealed class CollectingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Enqueue(formatter(state, exception));
            if (exception is not null)
            {
                messages.Enqueue(exception.ToString());
            }
        }
    }
}
