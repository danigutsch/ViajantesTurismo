using System.Net;
using System.Net.Http.Json;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>HTTP client for the Admin generated-documents API.</summary>
public sealed class DocumentsApiClient(HttpClient httpClient) : IDocumentsApiClient
{
    private const string RoutePrefix = "/api/v1/documents";
    private const string HtmlMediaType = "text/html";
    private static readonly DocumentsApiClientJsonContext Json = DocumentsApiClientJsonContext.Default;

    /// <inheritdoc />
    public async Task<GetDocumentDto?> GetDocumentById(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"{RoutePrefix}/{id}", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, ct).ConfigureAwait(false);
        return await ReadDocument(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<GetDocumentDto> GenerateContractDraft(Guid bookingId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/bookings/{bookingId}/contract-drafts", ct);

    /// <inheritdoc />
    public Task<GetDocumentDto> BeginReview(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/review", ct);

    /// <inheritdoc />
    public Task<GetDocumentDto> RequestChanges(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/changes-requested", ct);

    /// <inheritdoc />
    public async Task<GetDocumentDto> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentNullException.ThrowIfNull(dto);

        using var response = await httpClient.PatchAsJsonAsync(
            $"{RoutePrefix}/{documentId}/fields/{Uri.EscapeDataString(fieldId)}",
            dto,
            Json.UpdateDocumentFieldDto,
            ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
        return await ReadDocument(response, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<GetDocumentDto> Approve(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/approve", ct);

    /// <inheritdoc />
    public Task<GetDocumentDto> Finalize(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/finalize", ct);

    /// <inheritdoc />
    public Task<GetDocumentDto> Regenerate(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/regenerate", ct);

    /// <inheritdoc />
    public Task<GetDocumentDto> Void(Guid documentId, CancellationToken ct) =>
        PostCommand($"{RoutePrefix}/{documentId}/void", ct);

    /// <inheritdoc />
    public async Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(new Uri($"{RoutePrefix}/{documentId}/download", UriKind.Relative), ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, ct).ConfigureAwait(false);
        if (!string.Equals(response.Content.Headers.ContentType?.MediaType, HtmlMediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The document artifact response must be HTML.");
        }

        var content = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"{documentId:N}.html";
        return new DocumentArtifactResponse(content, fileName);
    }

    private async Task<GetDocumentDto> PostCommand(string requestUri, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync(new Uri(requestUri, UriKind.Relative), null, ct).ConfigureAwait(false);
        await EnsureSuccess(response, ct).ConfigureAwait(false);
        return await ReadDocument(response, ct).ConfigureAwait(false);
    }

    private static async Task<GetDocumentDto> ReadDocument(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync(Json.GetDocumentDto, ct).ConfigureAwait(false)
        ?? throw new InvalidOperationException("The document response body was empty.");

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken ct) =>
        await ContractHttpValidation.EnsureSuccessOrThrowValidationException(response, Json.ContractValidationProblemDto, ct).ConfigureAwait(false);
}
