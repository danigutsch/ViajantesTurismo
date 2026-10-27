namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the JSON media-type payload consumed from the tours OpenAPI document.
/// </summary>
/// <param name="Schema">The schema describing the media-type payload.</param>
internal sealed record ToursOpenApiMediaTypeDto(ToursOpenApiSchemaDto Schema);
