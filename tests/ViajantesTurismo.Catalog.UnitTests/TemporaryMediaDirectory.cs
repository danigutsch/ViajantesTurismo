namespace ViajantesTurismo.Catalog.UnitTests;

internal sealed class TemporaryMediaDirectory : IDisposable
{
    private TemporaryMediaDirectory(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TemporaryMediaDirectory Create()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"viajantes-media-{Guid.CreateVersion7():N}");
        Directory.CreateDirectory(path);

        return new TemporaryMediaDirectory(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
