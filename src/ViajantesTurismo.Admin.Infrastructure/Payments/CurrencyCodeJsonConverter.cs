using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Writes and reads <see cref="Currency"/> as its ISO 4217 code string in protected payment state.</summary>
internal sealed class CurrencyCodeJsonConverter : JsonConverter<Currency>
{
    /// <inheritdoc />
    public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && reader.GetString() is { Length: > 0 } code
            ? Currency.Restore(code)
            : throw new JsonException("A stored currency must be an ISO 4217 code string.");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        writer.WriteStringValue(value.Code);
    }
}
