using System.Text.Json;

namespace SharedKernel.Documentation;

/// <summary>
/// Generates configured Markdown blocks from repository files.
/// </summary>
public static class DocumentationGenerator
{

    /// <summary>
    /// Runs documentation generation from a JSON config file.
    /// </summary>
    public static DocumentationGenerationResult Run(string rootPath, string configPath, bool checkOnly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        var fullRootPath = Path.GetFullPath(rootPath);
        var fullConfigPath = Path.GetFullPath(configPath, fullRootPath);
        var config = JsonSerializer.Deserialize(File.ReadAllText(fullConfigPath), DocumentationGeneratorJsonContext.Default.DocumentationGeneratorConfig)
            ?? throw new InvalidOperationException($"Could not read documentation generator config: {configPath}");
        ValidateConfig(config);

        var replacements = config.Blocks
            .Select(block => KeyValuePair.Create(block.Name, GenerateBlock(fullRootPath, block)))
            .ToList();
        var updater = new GeneratedMarkdownUpdater(fullRootPath, config.DocsPath, config.GeneratorName);
        return new DocumentationGenerationResult(updater.Update(checkOnly, replacements));
    }

    private static void ValidateConfig(DocumentationGeneratorConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DocsPath))
        {
            throw new InvalidOperationException("Missing required docsPath.");
        }

        if (string.IsNullOrWhiteSpace(config.GeneratorName))
        {
            throw new InvalidOperationException("Missing required generatorName.");
        }

        if (config.Blocks is not { Count: > 0 })
        {
            throw new InvalidOperationException("Missing required blocks.");
        }

        if (config.Blocks.Select(block => block.Name).Distinct(StringComparer.Ordinal).Count() != config.Blocks.Count)
        {
            throw new InvalidOperationException("Generated block names must be unique.");
        }
    }

    private static string GenerateBlock(string rootPath, DocumentationGeneratorBlock block) => block.Kind switch
    {
        "mermaid-flowchart" => MermaidDiagram.Build(block.Flowchart, block.Lines),
        "project-references" => ProjectReferenceMermaidDiagram.Build(rootPath, block.SourcePath, ProjectFilter(block.ProjectFilter)),
        "apphost-resources" => AppHostMermaidDiagram.Build(Path.Combine(rootPath, block.SourcePath), block.Labels),
        "github-actions-jobs" => GitHubActionsMermaidDiagram.BuildJobs(Path.Combine(rootPath, block.SourcePath), block.TriggerLabel),
        "github-actions-workflows" => GitHubActionsMermaidDiagram.BuildWorkflowInventory(Path.Combine(rootPath, block.SourcePath), block.RootLabel, block.ExcludedWorkflowFileName),
        _ => throw new InvalidOperationException($"Unknown generated documentation block kind: {block.Kind}"),
    };

    private static Func<string[], bool> ProjectFilter(string value) => value switch
    {
        "src-excluding-sharedkernel" => path => path[0] == "src" && !path.Contains("SharedKernel", StringComparer.Ordinal),
        "sharedkernel" => path => path is ["src", "SharedKernel", ..],
        _ => throw new InvalidOperationException($"Unknown project filter: {value}"),
    };
}
