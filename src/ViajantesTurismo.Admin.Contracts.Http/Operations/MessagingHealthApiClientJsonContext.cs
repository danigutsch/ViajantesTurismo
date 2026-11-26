using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON metadata for the messaging-health client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(GetMessagingHealthDto))]
[JsonSerializable(typeof(GetCatalogDeliveryPageDto))]
[JsonSerializable(typeof(GetOutboxDeliveryPageDto))]
[JsonSerializable(typeof(ContractValidationProblemDto))]
internal sealed partial class MessagingHealthApiClientJsonContext : JsonSerializerContext;
