using System.Text.Json.Serialization;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Source-generated JSON metadata for protected payment persistence state.</summary>
[JsonSerializable(typeof(PaymentInstructionSnapshotState))]
[JsonSerializable(typeof(PaymentAccountState))]
[JsonSerializable(typeof(BookingFinancialState))]
[JsonSerializable(typeof(PaymentInstructionTermsState))]
internal sealed partial class PaymentPersistenceJsonContext : JsonSerializerContext;
