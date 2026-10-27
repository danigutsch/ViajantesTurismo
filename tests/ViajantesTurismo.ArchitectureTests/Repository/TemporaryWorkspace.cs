namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Temporary directory under the system temp path that is removed on dispose.</summary>
internal sealed class TemporaryWorkspace : IDisposable
{
    /// <summary>Creates a new uniquely named workspace directory.</summary>
    public TemporaryWorkspace()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "viajantes-architecture-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    /// <summary>Gets the absolute path of the workspace directory.</summary>
    public string RootPath
    {
        get;
    }

    /// <summary>Returns whether the file at <paramref name="relativePath"/> exists in the workspace.</summary>
    public bool FileExists(string relativePath) =>
        File.Exists(Path.Combine(RootPath, relativePath));

    /// <summary>Reads the text of the workspace file at <paramref name="relativePath"/>.</summary>
    public string ReadFile(string relativePath) =>
        File.ReadAllText(Path.Combine(RootPath, relativePath));

    /// <summary>Writes <paramref name="content"/> to <paramref name="relativePath"/>, creating parent directories.</summary>
    public void WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RootPath);
        File.WriteAllText(path, content);
    }

    /// <summary>Deletes the workspace file at <paramref name="relativePath"/>.</summary>
    public void DeleteFile(string relativePath) =>
        File.Delete(Path.Combine(RootPath, relativePath));

    /// <summary>Deletes the workspace directory and its contents.</summary>
    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
