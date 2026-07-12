namespace SharedKernel.RepoConfig.Tool;

internal static class Program
{
    public static int Main(string[] args) =>
        RepoConfigToolApplication.Run(args, Console.Out, Console.Error, Environment.CurrentDirectory);
}
