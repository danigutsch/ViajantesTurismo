namespace SharedKernel.Documentation.Tests;

internal sealed class TemporaryTextDocument : IDisposable
{
    public TemporaryTextDocument(string content)
    {
        Path = System.IO.Path.GetTempFileName();
        File.WriteAllText(Path, content);
    }

    public string Path { get; }

    public void Dispose() => File.Delete(Path);
}
