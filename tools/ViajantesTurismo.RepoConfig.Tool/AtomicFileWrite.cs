namespace ViajantesTurismo.RepoConfig.Tool;

internal sealed record AtomicFileWrite(string Path, string Content, string? ExpectedContent);
