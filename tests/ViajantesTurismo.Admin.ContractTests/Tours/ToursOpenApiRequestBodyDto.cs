namespace ViajantesTurismo.Admin.ContractTests.Tours;

/// <summary>
/// Represents the request-body content map used by the tours contract consumer.
/// </summary>
/// <param name="Content">The content-type map of the request body.</param>
internal sealed record ToursOpenApiRequestBodyDto(ToursOpenApiContentMapDto Content);
