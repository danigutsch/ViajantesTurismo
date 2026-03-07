using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// JSON serialization context for Native AOT compatibility.
/// Only top-level types used in API endpoints need to be listed.
/// </summary>
// Tour endpoints
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(IReadOnlyList<GetTourDto>))]
// Customer endpoints
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(IReadOnlyList<GetCustomerDto>))]
// Booking endpoints
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
[JsonSerializable(typeof(IReadOnlyList<GetBookingDto>))]
// Payment endpoints
[JsonSerializable(typeof(CreatePaymentDto))]
[JsonSerializable(typeof(GetPaymentDto))]
// Import endpoints
[JsonSerializable(typeof(ImportResultDto))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
