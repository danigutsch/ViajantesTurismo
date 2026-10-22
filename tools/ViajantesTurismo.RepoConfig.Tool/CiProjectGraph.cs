using System.Xml.Linq;

namespace ViajantesTurismo.RepoConfig.Tool;

internal static class CiProjectGraph
{
    public static string Resolve(string root, string path)
    {
        if (Path.IsPathRooted(path) || path.Replace('\\', '/').Split('/').Contains("..", StringComparer.Ordinal))
        {
            throw new ArgumentException("Project paths must stay inside the checkout.");
        }
        var full = Path.GetFullPath(path, root);
        for (var item = new FileInfo(full) as FileSystemInfo; item is not null && item.FullName != Path.GetFullPath(root); item = item is FileInfo file ? file.Directory : ((DirectoryInfo)item).Parent)
        {
            if (item.LinkTarget is not null)
            {
                throw new ArgumentException("Project paths cannot leave the checkout through symbolic links.");
            }
        }
        if (!File.Exists(full))
        {
            throw new ArgumentException("Project input does not exist in the checkout: " + path);
        }
        return full;
    }

    public static string Write(string root, string directory, string name, IEnumerable<string> projects)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".slnx");
        new XDocument(new XElement("Solution", projects.Select(project => new XElement("Project",
            new XAttribute("Path", Path.GetRelativePath(directory, Resolve(root, project))))))).Save(path);
        return path;
    }
}
