using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Source-generated JSON serialization context for the Bookings API client.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ContractValidationProblemDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(GetBookingDto[]))]
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
internal sealed partial class BookingsApiClientJsonContext : JsonSerializerContext;
