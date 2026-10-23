using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Admin.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
internal sealed partial class PaymentHttpProblemJsonContext : JsonSerializerContext;
