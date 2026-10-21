using System.Net;
using System.Net.Http.Json;
using System.Runtime.ExceptionServices;
using PandaBear.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>
/// HTTP client for catalog tour management endpoints.
/// </summary>
public sealed class CatalogToursApiClient(HttpClient httpClient) : ICatalogToursApiClient
{
    private const string RoutePrefix = "/api/v1/catalog";
    private static readonly CatalogToursApiClientJsonContext Json = CatalogToursApiClientJsonContext.Default;
    private static readonly object LocalUploadReadExceptionKey = new();

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogTourDto[]>> GetTours(CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/tours", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.CatalogTourDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return NormalizeTours(outcome);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogTourDto>> UpdatePresentation(
        Guid id,
        UpsertCatalogTourPresentationRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            new Uri($"{RoutePrefix}/tours/{id}/presentation", UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.UpsertCatalogTourPresentationRequest)
        };
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.OK,
            Json.CatalogTourDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return NormalizeTour(outcome);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> Publish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await ChangePublication(id, "publish", request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome> Unpublish(Guid id, CatalogTourPublicationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await ChangePublication(id, "unpublish", request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogTourDto>> GetTour(Guid id, CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/tours/{id}", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.CatalogTourDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return NormalizeTour(outcome);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogMediaImageDto>> GenerateMediaImageAccessibilityDraft(
        Guid id,
        PublicMediaImageAccessibilityDraftRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{RoutePrefix}/media/images/{id}/accessibility-draft", UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.PublicMediaImageAccessibilityDraftRequest)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.OK,
            Json.CatalogMediaImageDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(
        Guid id,
        CatalogTourImageUploadRequest request,
        IProgress<long>? progress,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (progress is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.ContentLength);
        }

        using var content = new MultipartFormDataContent();
        using HttpContent file = request.ContentLength > 0
            ? new ProgressStreamContent(new LocalUploadReadStream(request.Content), request.ContentLength, progress)
            : new StreamContent(new LocalUploadReadStream(request.Content));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
        content.Add(file, "file", request.FileName);
        content.Add(new StringContent(request.AltText), "altText");
        AddOptionalFormValue(content, "caption", request.Caption);
        AddOptionalFormValue(content, "attribution", request.Attribution);
        AddOptionalFormValue(content, "copyright", request.Copyright);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{RoutePrefix}/tours/{id}/images", UriKind.Relative))
        {
            Content = content
        };
        try
        {
            return await HttpOutcomeMapper.Send(
                httpClient,
                httpRequest,
                HttpStatusCode.Created,
                Json.CatalogMediaImageDto,
                Json.ContractValidationProblemDto,
                ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception.Data[LocalUploadReadExceptionKey] is IOException sourceException)
        {
            ExceptionDispatchInfo.Capture(sourceException).Throw();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogMediaImageDto[]>> GetTourImages(Guid id, CancellationToken ct)
    {
        var outcome = await HttpOutcomeMapper.Send(
            httpClient,
            new HttpRequestMessage(HttpMethod.Get, new Uri($"{RoutePrefix}/tours/{id}/images", UriKind.Relative)),
            HttpStatusCode.OK,
            Json.CatalogMediaImageDtoArray,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
        return NormalizeImages(outcome);
    }

    /// <inheritdoc />
    public async Task<HttpOutcome<CatalogMediaImageDto>> ReviewMediaImageAccessibility(
        Guid id,
        PublicMediaImageAccessibilityReviewRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var httpRequest = new HttpRequestMessage(
            HttpMethod.Put,
            new Uri($"{RoutePrefix}/media/images/{id}/accessibility-review", UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.PublicMediaImageAccessibilityReviewRequest)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.OK,
            Json.CatalogMediaImageDto,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PublicMediaObjectResponse?> GetMediaPreview(Guid id, int width, string format, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"{RoutePrefix}/media/images/{id}/preview/{width}/{Uri.EscapeDataString(format)}", UriKind.Relative));
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return new PublicMediaObjectResponse(response, content, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private async Task<HttpOutcome> ChangePublication(
        Guid id,
        string operation,
        CatalogTourPublicationRequest request,
        CancellationToken ct)
    {
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{RoutePrefix}/tours/{id}/{operation}", UriKind.Relative))
        {
            Content = JsonContent.Create(request, Json.CatalogTourPublicationRequest)
        };
        return await HttpOutcomeMapper.Send(
            httpClient,
            httpRequest,
            HttpStatusCode.NoContent,
            Json.ContractValidationProblemDto,
            ct).ConfigureAwait(false);
    }

    private static HttpOutcome<CatalogTourDto> NormalizeTour(HttpOutcome<CatalogTourDto> outcome)
    {
        return outcome is
        {
            Kind: HttpOutcomeKind.Succeeded,
            StatusCode: { } statusCode,
            Value.Version: < 1
        }
            ? HttpOutcomeMapper.Status<CatalogTourDto>(
                HttpOutcomeKind.MalformedBody,
                statusCode,
                "The catalog tour response body was invalid.")
            : outcome;
    }

    private static HttpOutcome<CatalogTourDto[]> NormalizeTours(HttpOutcome<CatalogTourDto[]> outcome)
    {
        if (outcome is not
            {
                Kind: HttpOutcomeKind.Succeeded,
                StatusCode: { } statusCode,
                Value: { } value
            })
        {
            return outcome;
        }

        var tours = value.OfType<CatalogTourDto>().ToArray();
        return tours.Any(tour => tour.Version < 1)
            ? HttpOutcomeMapper.Status<CatalogTourDto[]>(
                HttpOutcomeKind.MalformedBody,
                statusCode,
                "The catalog tour response body was invalid.")
            : outcome with
            {
                Value = tours
            };
    }

    private static HttpOutcome<CatalogMediaImageDto[]> NormalizeImages(
        HttpOutcome<CatalogMediaImageDto[]> outcome)
    {
        if (outcome is not
            {
                Kind: HttpOutcomeKind.Succeeded,
                StatusCode: { } statusCode,
                Value: { } value
            })
        {
            return outcome;
        }

        return value.Any(static image => image is null)
            ? HttpOutcomeMapper.Status<CatalogMediaImageDto[]>(
                HttpOutcomeKind.MalformedBody,
                statusCode,
                "The catalog image response body was invalid.")
            : outcome;
    }

    private static void AddOptionalFormValue(MultipartFormDataContent content, string name, string? value)
    {
        if (value is not null)
        {
            content.Add(new StringContent(value), name);
        }
    }

    private sealed class LocalUploadReadStream(Stream source) : Stream
    {
        public override bool CanRead => source.CanRead;

        public override bool CanSeek => source.CanSeek;

        public override bool CanWrite => source.CanWrite;

        public override long Length => source.Length;

        public override long Position
        {
            get => source.Position;
            set => source.Position = value;
        }

        public override void Flush() => source.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return source.Read(buffer, offset, count);
            }
            catch (IOException exception)
            {
                throw WrapLocalUploadReadException(exception);
            }
        }

        public override int Read(Span<byte> buffer)
        {
            try
            {
                return source.Read(buffer);
            }
            catch (IOException exception)
            {
                throw WrapLocalUploadReadException(exception);
            }
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            try
            {
                return await source.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
            }
            catch (IOException exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw WrapLocalUploadReadException(exception);
            }
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw WrapLocalUploadReadException(exception);
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);

        public override void SetLength(long value) => source.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => source.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }

            base.Dispose(disposing);
        }

        private static NotSupportedException WrapLocalUploadReadException(IOException sourceException)
        {
            var exception = new NotSupportedException("The local upload stream could not be read.", sourceException);
            exception.Data[LocalUploadReadExceptionKey] = sourceException;
            return exception;
        }
    }

    /// <inheritdoc />
    public Task<HttpOutcome<CatalogMediaImageDto>> UploadTourImage(Guid id, CatalogTourImageUploadRequest request, CancellationToken ct)
    {
        return UploadTourImage(id, request, null, ct);
    }
}
