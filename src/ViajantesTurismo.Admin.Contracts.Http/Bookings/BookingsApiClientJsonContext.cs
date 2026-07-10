using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
[JsonSerializable(typeof(CreatePaymentDto))]
internal sealed partial class BookingsApiClientJsonContext : JsonSerializerContext;
