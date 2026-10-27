using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

/// <summary>Source-generated JSON context used to serialize <see cref="JsonElement" /> when fingerprinting financial state.</summary>
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class FinancialFingerprintJsonContext : JsonSerializerContext;
