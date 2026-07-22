using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ViajantesTurismo.ServiceDefaults.Tests;

internal sealed class CollectingLoggerProvider(ConcurrentQueue<string> messages) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new CollectingLogger(messages);
    }

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
