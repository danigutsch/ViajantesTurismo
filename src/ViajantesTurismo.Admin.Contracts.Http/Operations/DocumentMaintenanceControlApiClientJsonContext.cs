using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON metadata for confirmed maintenance controls.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CreateDocumentMaintenanceControlDto))]
[JsonSerializable(typeof(ContractValidationProblemDto))]
internal sealed partial class DocumentMaintenanceControlApiClientJsonContext : JsonSerializerContext;
