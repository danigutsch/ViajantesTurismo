namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubIntakeTimeoutException : TimeoutException
{
    public GitHubIntakeTimeoutException()
        : base("GitHub intake timed out after 30 seconds.")
    {
    }

    public GitHubIntakeTimeoutException(string message) : base(message)
    {
    }

    public GitHubIntakeTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
