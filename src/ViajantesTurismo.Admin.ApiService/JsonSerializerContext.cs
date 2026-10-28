using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// JSON serialization context for Native AOT compatibility.
/// Only top-level types used in API endpoints need to be listed.
/// </summary>
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(IReadOnlyList<GetTourDto>))]
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(IReadOnlyList<GetCustomerDto>))]
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
[JsonSerializable(typeof(IReadOnlyList<GetBookingDto>))]
[JsonSerializable(typeof(ImportResultDto))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
