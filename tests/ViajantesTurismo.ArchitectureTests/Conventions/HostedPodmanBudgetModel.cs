using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reads Aspire container runtime model arguments without building or starting an application.</summary>
internal static class HostedPodmanBudgetModel
{
    /// <summary>Evaluates the container's runtime argument annotations in registration order.</summary>
    internal static async Task<string[]> Evaluate(ContainerResource container, CancellationToken ct)
    {
        var arguments = new List<object>();
        var context = new ContainerRuntimeArgsCallbackContext(arguments, ct);
        foreach (var annotation in container.Annotations.OfType<ContainerRuntimeArgsCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return [.. arguments.Cast<string>()];
    }
}
