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
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> GetDocumentById(Guid id, CancellationToken ct)
    {
        return await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.GetDocumentDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> GenerateContractDraft(Guid bookingId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/bookings/{bookingId}/contract-drafts", HttpStatusCode.Created, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> BeginReview(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/review", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> RequestChanges(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/changes-requested", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> UpdateField(Guid documentId, string fieldId, UpdateDocumentFieldDto dto, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentNullException.ThrowIfNull(dto);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            new Uri($"{RoutePrefix}/{documentId}/fields/{Uri.EscapeDataString(fieldId)}", UriKind.Relative))
        {
            Content = JsonContent.Create(dto, Json.UpdateDocumentFieldDto)
        };
        return await ContractCommandOutcome.Send(
            httpClient,
            request,
            HttpStatusCode.OK,
            Json.GetDocumentDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> Approve(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/approve", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> Finalize(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/finalize", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> Regenerate(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/regenerate", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ContractCommandOutcomeDto<GetDocumentDto>> Void(Guid documentId, CancellationToken ct) =>
        await PostCommand($"{RoutePrefix}/{documentId}/void", HttpStatusCode.OK, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<DocumentArtifactResponse?> DownloadFinalizedArtifact(Guid documentId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"{RoutePrefix}/{documentId}/download", UriKind.Relative));
        using var timeoutSource = httpClient.Timeout == Timeout.InfiniteTimeSpan
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutSource?.CancelAfter(httpClient.Timeout);
        var requestToken = timeoutSource?.Token ?? ct;

        try
        {
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                requestToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            await ContractHttpValidation.EnsureSuccessOrThrowValidationException(
                response,
                Json.ContractValidationProblemDto,
                requestToken).ConfigureAwait(false);
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, HtmlMediaType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The document artifact response must be HTML.");
            }

            var content = await response.Content.ReadAsByteArrayAsync(requestToken).ConfigureAwait(false);
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"{documentId:N}.html";
            return new DocumentArtifactResponse(content, fileName);
        }
        catch (OperationCanceledException exception) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, ct);
        }
    }

    private async Task<ContractCommandOutcomeDto<GetDocumentDto>> PostCommand(
        string requestUri,
        HttpStatusCode successStatusCode,
        CancellationToken ct)
    {
        return await ContractCommandOutcome.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Post, new Uri(requestUri, UriKind.Relative)),
            successStatusCode,
            Json.GetDocumentDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }
}
