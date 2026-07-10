using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(ImportResultDto))]
internal sealed partial class CustomersApiClientJsonContext : JsonSerializerContext;
