using System.Text.Json;
using System.Text.Json.Serialization;
using PandaBear.BuildingBlocks;

namespace ViajantesTurismo.Admin.Infrastructure.Payments;

/// <summary>Writes and reads <see cref="Currency"/> as the numeric enum value that protected payment state has always stored.</summary>
internal sealed class StoredCurrencyJsonConverter : JsonConverter<Currency>
{
    /// <inheritdoc />
    public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var ordinal)
            ? StoredCurrency.FromOrdinal(ordinal)
            : throw new JsonException("A stored currency must be an integer.");

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(StoredCurrency.ToOrdinal(value));
}
