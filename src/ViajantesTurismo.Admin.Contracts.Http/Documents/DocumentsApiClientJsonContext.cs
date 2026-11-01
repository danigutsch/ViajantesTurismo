using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Documents API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(CreateDocumentHoldDto))]
[JsonSerializable(typeof(GetDocumentDto))]
[JsonSerializable(typeof(GetDocumentHoldDto))]
[JsonSerializable(typeof(IReadOnlyList<GetDocumentHoldDto>))]
[JsonSerializable(typeof(GetDocumentHoldOverviewPageDto))]
[JsonSerializable(typeof(ReleaseDocumentHoldDto))]
[JsonSerializable(typeof(UpdateDocumentFieldDto))]
internal sealed partial class DocumentsApiClientJsonContext : JsonSerializerContext;
