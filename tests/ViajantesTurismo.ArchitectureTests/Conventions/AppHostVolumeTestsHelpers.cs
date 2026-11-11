using Aspire.Hosting.ApplicationModel;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Reads container lifetimes from the AppHost model.</summary>
internal static class AppHostVolumeTestsHelpers
{
    /// <summary>Gets whether the last lifetime annotation of a resource keeps its container between runs.</summary>
    public static bool IsPersistent(IResource resource) =>
        resource.Annotations.OfType<ContainerLifetimeAnnotation>().LastOrDefault()?.Lifetime == ContainerLifetime.Persistent;
}
