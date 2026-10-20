namespace ViajantesTurismo.RepoConfig.Tool;

internal static class RepoConfigPaths
{
    public static string RelativeTo(string rootPath, string fullPath) =>
        Normalize(Path.GetRelativePath(rootPath, fullPath));

    public static string Normalize(string path) =>
        path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
}
