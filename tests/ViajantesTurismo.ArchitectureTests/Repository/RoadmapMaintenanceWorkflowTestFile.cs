namespace ViajantesTurismo.ArchitectureTests.Repository;

internal static class RoadmapMaintenanceWorkflowTestFile
{
    public static string Content => File.ReadAllText(Path.Combine(
        RepositoryTestProcess.GetRepositoryRoot(),
        ".github",
        "workflows",
        "roadmap-maintenance.yml"));
}
