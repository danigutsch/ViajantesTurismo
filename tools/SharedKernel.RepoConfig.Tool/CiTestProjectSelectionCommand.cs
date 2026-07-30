using System.Globalization;

namespace SharedKernel.RepoConfig.Tool;

internal static class CiTestProjectSelectionCommand
{
    private const string Usage = "Usage: sharedkernel-repo select-ci-test-projects --mode <full|merge-base|direct> [--base <sha> --head <sha>] [--output-directory <path>] [--github-output <path>] [--root <path>]";

    public static async Task<int> Run(
        string[] args,
        TextWriter output,
        TextWriter error,
        string workingDirectory,
        CancellationToken ct)
    {
        if (!TryParseOptions(args, workingDirectory, error, out var options))
        {
            return 2;
        }

        CiTestProjectSelector.ValidateCanonicalSliceManifests(options.RootPath);

        var selection = await Select(options, error, ct).ConfigureAwait(false);

        Directory.CreateDirectory(options.OutputDirectory);
        foreach (var slice in selection.SelectedProjectsBySlice)
        {
            await File.WriteAllLinesAsync(
                Path.Combine(options.OutputDirectory, $"{slice.Key}.txt"),
                slice.Value,
                ct).ConfigureAwait(false);
        }

        await File.WriteAllLinesAsync(
            Path.Combine(options.OutputDirectory, "openapi-tool-windows.txt"),
            selection.OpenApiToolWindowsRequired ? CiTestProjectSelector.OpenApiWindowsProjects : [],
            ct).ConfigureAwait(false);

        var selectedProjectCount = selection.SelectedProjectsBySlice.Values.Sum(static projects => projects.Count);
        var githubOutputs = new List<string>
        {
            FormatOutput("build_required", selection.BuildRequired),
            FormatOutput("openapi_tool_windows_required", selection.OpenApiToolWindowsRequired),
            FormatOutput("selection_fallback", selection.FallbackToFullValidation),
            $"selected_test_project_count={selectedProjectCount.ToString(CultureInfo.InvariantCulture)}"
        };
        githubOutputs.AddRange(selection.SelectedProjectsBySlice.Select(slice =>
            FormatOutput($"{slice.Key.Replace('-', '_')}_required", slice.Value.Count > 0)));
        var fastValidationRequired = selection.SelectedProjectsBySlice["fast-validation-1"].Count > 0
            || selection.SelectedProjectsBySlice["fast-validation-2"].Count > 0;
        githubOutputs.Add(FormatOutput("fast_validation_required", fastValidationRequired));

        if (options.GitHubOutputPath is not null)
        {
            var outputDirectory = Path.GetDirectoryName(options.GitHubOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            await File.AppendAllLinesAsync(options.GitHubOutputPath, githubOutputs, ct).ConfigureAwait(false);
        }

        await output.WriteLineAsync(
            $"Selected {selectedProjectCount.ToString(CultureInfo.InvariantCulture)} CI test project(s).".AsMemory(),
            ct).ConfigureAwait(false);
        return 0;
    }

    private static async Task<CiTestProjectSelection> Select(
        Options options,
        TextWriter error,
        CancellationToken ct)
    {
        if (options.Mode == "full")
        {
            CiTestProjectSelector.ValidateCanonicalSliceMembership(options.RootPath);
            return CiTestProjectSelector.Select(options.RootPath, [], fullValidation: true);
        }

        try
        {
            CiTestProjectSelector.ValidateCanonicalSliceMembership(options.RootPath);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or System.Xml.XmlException)
        {
            return await CreateFallbackSelection(options.RootPath, error, exception, ct).ConfigureAwait(false);
        }

        try
        {
            var changedPaths = await CiChangedPathReader.Read(
                options.RootPath,
                options.BaseSha ?? string.Empty,
                options.HeadSha ?? string.Empty,
                useMergeBase: options.Mode == "merge-base",
                ct).ConfigureAwait(false);
            return CiTestProjectSelector.Select(options.RootPath, changedPaths, fullValidation: false);
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException)
        {
            return await CreateFallbackSelection(options.RootPath, error, exception, ct).ConfigureAwait(false);
        }
    }

    private static async Task<CiTestProjectSelection> CreateFallbackSelection(
        string rootPath,
        TextWriter error,
        Exception exception,
        CancellationToken ct)
    {
        await error.WriteLineAsync(
            $"CI test selection fell back to full validation: {exception.Message}".AsMemory(),
            ct).ConfigureAwait(false);
        return CiTestProjectSelector.Select(rootPath, [], fullValidation: true) with
        {
            FallbackToFullValidation = true
        };
    }

    private static bool TryParseOptions(
        string[] args,
        string workingDirectory,
        TextWriter error,
        out Options options)
    {
        var rootPath = Path.GetFullPath(workingDirectory);
        string? mode = null;
        string? baseSha = null;
        string? headSha = null;
        string? outputDirectory = null;
        string? githubOutputPath = null;
        var index = 0;

        while (index < args.Length)
        {
            if (index + 1 >= args.Length)
            {
                options = new Options(string.Empty, string.Empty, null, null, string.Empty, null);
                return WriteUsageError(error, $"Missing required value for {args[index]}.");
            }

            var value = args[index + 1];
            switch (args[index])
            {
                case "--root":
                    rootPath = Path.GetFullPath(value, workingDirectory);
                    break;
                case "--mode":
                    mode = value;
                    break;
                case "--base":
                    baseSha = value;
                    break;
                case "--head":
                    headSha = value;
                    break;
                case "--output-directory":
                    outputDirectory = value;
                    break;
                case "--github-output":
                    githubOutputPath = value;
                    break;
                default:
                    options = new Options(string.Empty, string.Empty, null, null, string.Empty, null);
                    return WriteUsageError(error, $"Unknown CI test selection argument: {args[index]}");
            }

            index += 2;
        }

        if (mode is not ("full" or "merge-base" or "direct"))
        {
            options = new Options(string.Empty, string.Empty, null, null, string.Empty, null);
            return WriteUsageError(error, "CI test selection requires --mode full, merge-base, or direct.");
        }

        if (mode != "full" && (string.IsNullOrWhiteSpace(baseSha) || string.IsNullOrWhiteSpace(headSha)))
        {
            options = new Options(string.Empty, string.Empty, null, null, string.Empty, null);
            return WriteUsageError(error, "Selective CI test selection requires --base and --head.");
        }

        outputDirectory = Path.GetFullPath(
            outputDirectory ?? Path.Combine("TestResults", "selected-ci-test-slices"),
            rootPath);
        if (githubOutputPath is not null)
        {
            githubOutputPath = Path.GetFullPath(githubOutputPath, rootPath);
        }

        options = new Options(rootPath, mode, baseSha, headSha, outputDirectory, githubOutputPath);
        return true;
    }

    private static bool WriteUsageError(TextWriter error, string message)
    {
        error.WriteLine(message);
        error.WriteLine(Usage);
        return false;
    }

    private static string FormatOutput(string name, bool value) =>
        $"{name}={(value ? "true" : "false")}";

    private sealed record Options(
        string RootPath,
        string Mode,
        string? BaseSha,
        string? HeadSha,
        string OutputDirectory,
        string? GitHubOutputPath);
}
