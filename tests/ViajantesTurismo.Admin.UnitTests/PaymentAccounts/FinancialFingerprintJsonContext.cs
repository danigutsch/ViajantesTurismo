using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[JsonSerializable(typeof(JsonElement))]
internal sealed partial class FinancialFingerprintJsonContext : JsonSerializerContext;
