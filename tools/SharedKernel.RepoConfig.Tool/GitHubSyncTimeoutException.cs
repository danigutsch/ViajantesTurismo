namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubSyncTimeoutException : TimeoutException
{
    public GitHubSyncTimeoutException()
        : base("GitHub sync timed out after 30 seconds.")
    {
    }

    public GitHubSyncTimeoutException(string message) : base(message)
    {
    }

    public GitHubSyncTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
