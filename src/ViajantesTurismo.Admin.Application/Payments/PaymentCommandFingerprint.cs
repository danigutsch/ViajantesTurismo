using ViajantesTurismo.Admin.Application.Idempotency;
using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Fingerprints meaningful typed inputs with canonical decimal numbers and no retained financial payload.</summary>
internal static class PaymentCommandFingerprint
{
    public static string Create<T>(Guid bookingId, Guid? targetId, string actorId, T input, JsonTypeInfo<T> type)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(input, type);
        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            using var document = JsonDocument.Parse(bytes);
            using (var writer = new Utf8JsonWriter(buffer))
            {
                Canonicalize(document.RootElement, writer);
            }
            return ApplicationRequestFingerprint.Create(bookingId.ToString("N"), targetId?.ToString("N") ?? string.Empty, actorId, Encoding.UTF8.GetString(buffer.WrittenSpan));
        }
        finally { CryptographicOperations.ZeroMemory(bytes); buffer.Clear(); }
    }

    private static void Canonicalize(JsonElement value, Utf8JsonWriter writer)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Canonicalize(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var element in value.EnumerateArray())
                {
                    Canonicalize(element, writer);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number when value.TryGetDecimal(out var number):
                writer.WriteRawValue(number.ToString("G29", CultureInfo.InvariantCulture));
                break;
            default:
                value.WriteTo(writer);
                break;
        }
    }
}
