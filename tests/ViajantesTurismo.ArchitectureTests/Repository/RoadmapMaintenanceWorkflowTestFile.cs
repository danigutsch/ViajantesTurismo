namespace ViajantesTurismo.ArchitectureTests.Repository;

/// <summary>Provides the checked-in roadmap maintenance GitHub workflow for tests.</summary>
internal static class RoadmapMaintenanceWorkflowTestFile
{
    /// <summary>Gets the text of <c>.github/workflows/roadmap-maintenance.yml</c>.</summary>
    public static string Content => File.ReadAllText(Path.Combine(
        RepositoryTestProcess.GetRepositoryRoot(),
        ".github",
        "workflows",
        "roadmap-maintenance.yml"));
}
