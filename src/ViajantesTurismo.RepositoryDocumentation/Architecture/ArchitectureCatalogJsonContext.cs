using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.RepositoryDocumentation.Architecture;

/// <summary>Source-generated JSON metadata for the repository's architecture catalog.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(ArchitectureCatalog))]
internal sealed partial class ArchitectureCatalogJsonContext : JsonSerializerContext;
