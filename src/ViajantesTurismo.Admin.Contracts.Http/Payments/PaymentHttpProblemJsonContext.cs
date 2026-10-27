using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON serialization context for Payment HTTP validation problem responses.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
internal sealed partial class PaymentHttpProblemJsonContext : JsonSerializerContext;
