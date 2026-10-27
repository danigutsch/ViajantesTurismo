namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Temporary directory under the system temp path that is deleted recursively on dispose.</summary>
internal sealed class TemporaryMediaDirectory : IDisposable
{
    private TemporaryMediaDirectory(string path)
    {
        Path = path;
    }

    /// <summary>Gets the absolute path of the directory.</summary>
    public string Path
    {
        get;
    }

    /// <summary>Creates a uniquely named directory and returns the wrapper.</summary>
    public static TemporaryMediaDirectory Create()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"viajantes-media-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(path);

        return new TemporaryMediaDirectory(path);
    }

    /// <summary>Deletes the directory and its contents if it still exists.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
