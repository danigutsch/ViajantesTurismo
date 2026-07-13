using System.Text.Json.Serialization;

namespace SharedKernel.RepoConfig.Tool;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GitHubProjectResponse))]
internal sealed partial class GitHubProjectJsonContext : JsonSerializerContext;
