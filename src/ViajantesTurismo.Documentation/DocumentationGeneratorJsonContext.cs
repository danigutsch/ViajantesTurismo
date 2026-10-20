using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Documentation;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(DocumentationGeneratorConfig))]
internal sealed partial class DocumentationGeneratorJsonContext : JsonSerializerContext
{
}
