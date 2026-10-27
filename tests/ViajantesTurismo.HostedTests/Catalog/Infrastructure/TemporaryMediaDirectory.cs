namespace ViajantesTurismo.Catalog.InfrastructureTests;

/// <summary>Temporary directory for media files that is deleted when disposed.</summary>
internal sealed class TemporaryMediaDirectory : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();

    /// <summary>Gets the full path of the temporary directory.</summary>
    public string Path => directory.FullName;

    /// <summary>Deletes the directory and its contents.</summary>
    public void Dispose()
    {
        directory.Delete(recursive: true);
    }
}
