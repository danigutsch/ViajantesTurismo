using System.Text.Json.Nodes;

namespace ViajantesTurismo.Admin.ContractTests;

/// <summary>Reads and verifies resource links in the Admin OpenAPI snapshots.</summary>
internal static class AdminHypermediaOpenApiAssertions
{
    /// <summary>Gets the declared operation response.</summary>
    internal static JsonNode GetResponse(JsonObject paths, string path, string method, string status) =>
        paths[path].ShouldNotBeNull()[method].ShouldNotBeNull()["responses"].ShouldNotBeNull()[status].ShouldNotBeNull();

    /// <summary>Gets the JSON response's payload schema.</summary>
    internal static JsonNode GetJsonSchema(JsonNode response) =>
        response["content"].ShouldNotBeNull()["application/json"].ShouldNotBeNull()["schema"].ShouldNotBeNull();

    /// <summary>Verifies that typed consumers can receive the optional link array.</summary>
    internal static void AssertLinkProperty(JsonNode schema)
    {
        schema["required"].ShouldNotBeNull().AsArray().Select(field => field.ShouldNotBeNull().GetValue<string>())
            .ShouldNotContain("links");
        var links = schema["properties"].ShouldNotBeNull()["links"].ShouldNotBeNull();
        links["type"].ShouldNotBeNull().AsArray()
            .Select(type => type.ShouldNotBeNull().GetValue<string>()).ShouldContain("array");
        links["items"].ShouldNotBeNull()["$ref"].ShouldNotBeNull().GetValue<string>()
            .ShouldBe("#/components/schemas/HypermediaLink");
    }
}
