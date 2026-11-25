using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Customers API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(GetCustomerDto[]))]
[JsonSerializable(typeof(GetCustomerPageDto))]
[JsonSerializable(typeof(CustomerOptionSearchDto))]
[JsonSerializable(typeof(GetCustomerOptionsDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(ImportResultDto))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Guid?[]), TypeInfoPropertyName = "NullableGuidArray")]
internal sealed partial class CustomersApiClientJsonContext : JsonSerializerContext;
