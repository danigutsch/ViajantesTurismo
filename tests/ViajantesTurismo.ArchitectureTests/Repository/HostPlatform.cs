namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Reports facts about the operating system hosting the test run.</summary>
internal static class HostPlatform
{
    /// <summary>Gets a value indicating whether the tests are running on Windows.</summary>
    public static bool IsWindows => OperatingSystem.IsWindows();
}
