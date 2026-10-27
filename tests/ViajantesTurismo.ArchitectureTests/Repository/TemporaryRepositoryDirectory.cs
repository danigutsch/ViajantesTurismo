namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Deletes temporary repository paths, clearing read-only attributes first.</summary>
internal static class TemporaryRepositoryDirectory
{
    /// <summary>Deletes the directory tree at <paramref name="path"/> if it exists, making files writable first.</summary>
    public static void Delete(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            IgnoreInaccessible = false
        };
        foreach (var filePath in Directory.EnumerateFiles(path, "*", options))
        {
            MakeWritable(filePath);
        }

        Directory.Delete(path, recursive: true);
    }

    /// <summary>Deletes the file at <paramref name="path"/>, clearing its read-only attribute if present.</summary>
    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            MakeWritable(path);
        }

        File.Delete(path);
    }

    private static void MakeWritable(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }
}
