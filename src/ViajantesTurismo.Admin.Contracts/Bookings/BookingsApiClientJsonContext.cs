using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;

namespace ViajantesTurismo.Admin.Contracts;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
[JsonSerializable(typeof(CreatePaymentDto))]
internal sealed partial class BookingsApiClientJsonContext : JsonSerializerContext;
