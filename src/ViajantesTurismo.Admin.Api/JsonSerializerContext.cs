using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api;

/// <summary>
/// JSON serialization context for Native AOT compatibility.
/// Only top-level types used in API endpoints need to be listed.
/// </summary>
[JsonSerializable(typeof(CreateTourDto))]
[JsonSerializable(typeof(UpdateTourDto))]
[JsonSerializable(typeof(GetTourDto))]
[JsonSerializable(typeof(GetTourRetirementDto))]
[JsonSerializable(typeof(CreateCustomerDto))]
[JsonSerializable(typeof(UpdateCustomerDto))]
[JsonSerializable(typeof(GetCustomerDto))]
[JsonSerializable(typeof(CustomerDetailsDto))]
[JsonSerializable(typeof(CreateBookingDto))]
[JsonSerializable(typeof(GetBookingDto))]
[JsonSerializable(typeof(UpdateBookingDetailsDto))]
[JsonSerializable(typeof(UpdateBookingDiscountDto))]
[JsonSerializable(typeof(UpdateBookingNotesDto))]
[JsonSerializable(typeof(ImportResultDto))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Guid?[]))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
