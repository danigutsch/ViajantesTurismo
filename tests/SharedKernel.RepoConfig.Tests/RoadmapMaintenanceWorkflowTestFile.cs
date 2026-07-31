namespace SharedKernel.RepoConfig.Tests;

internal static class RoadmapMaintenanceWorkflowTestFile
{
    public static string Content => File.ReadAllText(Path.Combine(
        CiTestSelectionTestProcess.GetRepositoryRoot(),
        ".github",
        "workflows",
        "roadmap-maintenance.yml"));
}
