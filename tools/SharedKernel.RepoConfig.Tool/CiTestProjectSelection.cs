namespace SharedKernel.RepoConfig.Tool;

internal sealed record CiTestProjectSelection(
    bool BuildRequired,
    bool FallbackToFullValidation,
    bool OpenApiToolWindowsRequired,
    IReadOnlyDictionary<string, IReadOnlyList<string>> SelectedProjectsBySlice);
