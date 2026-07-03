using System.Text.Json;
using System.Text.Json.Serialization;
using ViajantesTurismo.Common.Contracts;

namespace ViajantesTurismo.Admin.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(ImportResultDto))]
internal sealed partial class CustomersApiClientJsonContext : JsonSerializerContext;
