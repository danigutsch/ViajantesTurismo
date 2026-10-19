using System.Runtime.InteropServices;
using SharedKernel.PlaywrightMcp.Tool;

using var cancellation = new CancellationTokenSource();
void cancelHandler(object? _, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
}
Console.CancelKeyPress += cancelHandler;

using var terminationSignal = OperatingSystem.IsWindows()
    ? null
    : PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
    {
        context.Cancel = true;
        cancellation.Cancel();
    });

try
{
    return await PlaywrightMcpApplication.Run(args, Console.Error, cancellation.Token).ConfigureAwait(false);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
