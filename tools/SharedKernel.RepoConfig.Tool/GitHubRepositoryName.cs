namespace SharedKernel.RepoConfig.Tool;

internal static class GitHubRepositoryName
{
    public static bool IsValid(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var parts = value.Split('/');
        return parts.Length == 2
            && parts.All(part => !string.IsNullOrWhiteSpace(part))
            && parts.All(part => !part.Any(char.IsWhiteSpace));
    }
}
