using System.Runtime.ExceptionServices;

namespace SharedKernel.Documentation.Tests;

internal sealed class PathTooLongExceptionCapture : IDisposable
{
    private readonly int threadId = Environment.CurrentManagedThreadId;

    public PathTooLongExceptionCapture()
    {
        AppDomain.CurrentDomain.FirstChanceException += Capture;
    }

    public PathTooLongException? FirstException { get; private set; }

    public void Dispose()
    {
        AppDomain.CurrentDomain.FirstChanceException -= Capture;
    }

    private void Capture(object? sender, FirstChanceExceptionEventArgs eventArgs)
    {
        if (Environment.CurrentManagedThreadId == threadId
            && eventArgs.Exception is PathTooLongException exception)
        {
            FirstException ??= exception;
        }
    }
}
