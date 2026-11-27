namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Applies the selected local Podman slice to test-owned container resources.</summary>
internal static class PodmanContainerBudget
{
    private const string SliceVariable = "VT_TEST_PODMAN_CGROUP_PARENT_SLICE";

    /// <summary>Appends the same cgroup parent to each container when local isolated Podman validation requests it.</summary>
    internal static void Apply(IDistributedApplicationBuilder builder)
    {
        var slice = Environment.GetEnvironmentVariable(SliceVariable);
        if (slice is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(slice)
            || !string.Equals(Environment.GetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME"), "podman", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A Hosted test cgroup parent requires a user slice and the Podman container runtime.");
        }

        foreach (var container in builder.Resources.OfType<ContainerResource>())
        {
            _ = builder.CreateResourceBuilder(container).WithContainerRuntimeArgs("--cgroup-parent", slice);
        }
    }
}
