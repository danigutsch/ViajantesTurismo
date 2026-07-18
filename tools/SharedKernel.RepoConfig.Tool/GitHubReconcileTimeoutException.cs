namespace SharedKernel.RepoConfig.Tool;

internal sealed class GitHubReconcileTimeoutException : TimeoutException
{
    public GitHubReconcileTimeoutException()
        : base("GitHub reconciliation timed out after 30 seconds.")
    {
    }

    public GitHubReconcileTimeoutException(string message) : base(message)
    {
    }

    public GitHubReconcileTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
