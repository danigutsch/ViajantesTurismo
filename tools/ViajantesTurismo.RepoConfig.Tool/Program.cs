namespace ViajantesTurismo.RepoConfig.Tool;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        void Cancel(object? sender, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        }
        Console.CancelKeyPress += Cancel;
        try
        {
            return await RepoConfigToolApplication.Run(args, Console.Out, Console.Error, Environment.CurrentDirectory, cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return 130;
        }
        finally
        {
            Console.CancelKeyPress -= Cancel;
        }
    }
}
