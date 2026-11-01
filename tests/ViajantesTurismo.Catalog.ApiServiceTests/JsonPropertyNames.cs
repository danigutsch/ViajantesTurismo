using System.Text.Json;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>Collects JSON property names from response bodies.</summary>
internal static class JsonPropertyNames
{
    /// <summary>Returns every property name at any depth in <paramref name="json"/>.</summary>
    public static IReadOnlyList<string> Collect(string json)
    {
        using var document = JsonDocument.Parse(json);
        var names = new List<string>();
        Collect(document.RootElement, names);
        return names;
    }

    private static void Collect(JsonElement element, List<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    names.Add(property.Name);
                    Collect(property.Value, names);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, names);
                }

                break;
        }
    }
}
