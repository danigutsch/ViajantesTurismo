namespace ViajantesTurismo.ArchitectureTests.Repository;

internal static class HostPlatform
{
    public static bool IsWindows => OperatingSystem.IsWindows();
}
