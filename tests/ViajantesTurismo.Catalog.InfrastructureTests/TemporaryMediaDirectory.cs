namespace ViajantesTurismo.Catalog.InfrastructureTests;

internal sealed class TemporaryMediaDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();

    public string Path => directory.FullName;

    public void Dispose()
    {
        directory.Delete(recursive: true);
    }
}