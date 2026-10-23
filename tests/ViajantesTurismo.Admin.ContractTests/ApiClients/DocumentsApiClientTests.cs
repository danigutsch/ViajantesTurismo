using System.Net;
using System.Net.Http.Headers;
using System.Text;
using PandaBear.HttpClients;
using PandaBear.Testing.Contracts;
using ViajantesTurismo.Admin.Contracts.Http;

namespace ViajantesTurismo.Admin.ContractTests.ApiClients;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class DocumentsApiClientTests
{
    private const string DocumentJson = """
        {
          "id":"11111111-1111-1111-1111-111111111111",
          "bookingId":"22222222-2222-2222-2222-222222222222",
          "revision":1,
          "templateId":"tour-service-contract",
          "templateVersion":"1",
          "sourceVersion":"SOURCE-VERSION",
          "status":0,
          "fields":[{"fieldId":"greeting","label":"Greeting","renderedValue":"Dear customer","isEditable":true}],
          "createdAt":"2026-07-16T09:00:00Z",
          "updatedAt":"2026-07-16T09:00:00Z",
          "hasFinalizedArtifact":false
        }
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Separate_document_operations_send_their_distinct_caller_owned_keys(bool regenerate)
    {
        // Arrange
        List<string> keys = [];
        var requestsHaveNoBody = true;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            keys.Add(request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.ShouldHaveSingleItem() : string.Empty);
            requestsHaveNoBody &= request.Content is null;
            return ContractHttpClientTestHelper.JsonResponse(DocumentJson, regenerate ? HttpStatusCode.OK : HttpStatusCode.Created);
        });
        var sut = new DocumentsApiClient(httpClient);
        var id = Guid.CreateVersion7();
        var firstKey = Guid.CreateVersion7().ToString("N");
        var secondKey = Guid.CreateVersion7().ToString("N");

        // Act
        var first = regenerate
            ? await sut.Regenerate(id, firstKey, TestContext.Current.CancellationToken)
            : await sut.GenerateContractDraft(id, firstKey, TestContext.Current.CancellationToken);
        var second = regenerate
            ? await sut.Regenerate(id, secondKey, TestContext.Current.CancellationToken)
            : await sut.GenerateContractDraft(id, secondKey, TestContext.Current.CancellationToken);

        // Assert
        first.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        second.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        keys.Count.ShouldBe(2);
        keys[0].ShouldBe(firstKey);
        keys[1].ShouldBe(secondKey);
        requestsHaveNoBody.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Caller_provided_key_is_preserved_when_the_same_command_is_retried(bool regenerate)
    {
        // Arrange
        const string key = "stable-document-command-key";
        List<string> keys = [];
        var requestsHaveNoBody = true;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            keys.Add(request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.ShouldHaveSingleItem() : string.Empty);
            requestsHaveNoBody &= request.Content is null;
            return ContractHttpClientTestHelper.JsonResponse(DocumentJson, regenerate ? HttpStatusCode.OK : HttpStatusCode.Created);
        });
        var sut = new DocumentsApiClient(httpClient);
        var id = Guid.CreateVersion7();

        // Act
        var first = regenerate
            ? await sut.Regenerate(id, key, TestContext.Current.CancellationToken)
            : await sut.GenerateContractDraft(id, key, TestContext.Current.CancellationToken);
        var second = regenerate
            ? await sut.Regenerate(id, key, TestContext.Current.CancellationToken)
            : await sut.GenerateContractDraft(id, key, TestContext.Current.CancellationToken);

        // Assert
        first.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        second.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        keys.ShouldBeEquivalentTo(key, key);
        requestsHaveNoBody.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    public async Task Caller_provided_key_must_not_be_blank(bool regenerate, string? key)
    {
        // Arrange
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new DocumentsApiClient(httpClient);
        var id = Guid.CreateVersion7();
        Func<Task> action = async () => _ = regenerate
            ? await sut.Regenerate(id, key!, TestContext.Current.CancellationToken)
            : await sut.GenerateContractDraft(id, key!, TestContext.Current.CancellationToken);

        // Act
        var exception = await action.ShouldThrowAssignableTo<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("idempotencyKey");
    }

    [Fact]
    public async Task Non_creation_document_commands_do_not_send_an_idempotency_key()
    {
        // Arrange
        var hasIdempotencyKey = false;
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            hasIdempotencyKey = request.Headers.Contains("Idempotency-Key");
            return ContractHttpClientTestHelper.JsonResponse(DocumentJson);
        });
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.BeginReview(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.Succeeded);
        hasIdempotencyKey.ShouldBeFalse();
    }

    [Fact]
    public async Task GetDocumentById_returns_not_found_outcome()
    {
        // Arrange
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.GetDocumentById(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetDocumentById_requests_the_document_endpoint_and_returns_the_document()
    {
        // Arrange
        var requestPath = string.Empty;
        var documentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return ContractHttpClientTestHelper.JsonResponse(DocumentJson);
        });
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var outcome = await sut.GetDocumentById(documentId, TestContext.Current.CancellationToken);

        // Assert
        var documentDto = outcome.Value.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/documents/11111111-1111-1111-1111-111111111111");
        documentDto.TemplateId.ShouldBe("tour-service-contract");
    }

    [Fact]
    public async Task DownloadFinalizedArtifact_returns_html_bytes_without_a_storage_url()
    {
        // Arrange
        var requestPath = string.Empty;
        var documentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = ContractHttpClientTestHelper.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            var content = new StringContent("<html><body>contract</body></html>", Encoding.UTF8, "text/html");
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = "11111111-1111-1111-1111-111111111111.html"
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        });
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var artifact = await sut.DownloadFinalizedArtifact(documentId, TestContext.Current.CancellationToken);

        // Assert
        var downloadedArtifact = artifact.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/documents/11111111-1111-1111-1111-111111111111/download");
        downloadedArtifact.FileName.ShouldBe("11111111-1111-1111-1111-111111111111.html");
        Encoding.UTF8.GetString(downloadedArtifact.Content.Span).ShouldBe("<html><body>contract</body></html>");
    }

    [Fact]
    public async Task DownloadFinalizedArtifact_returns_null_when_the_artifact_is_missing()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new DocumentsApiClient(httpClient);

        // Act
        var artifact = await sut.DownloadFinalizedArtifact(documentId, TestContext.Current.CancellationToken);

        // Assert
        artifact.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadFinalizedArtifact_rejects_a_non_html_response()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not html", Encoding.UTF8, "application/json")
            });
        var sut = new DocumentsApiClient(httpClient);
        Func<Task> download = async () => await sut.DownloadFinalizedArtifact(documentId, TestContext.Current.CancellationToken);

        // Act
        var exception = await download.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("The document artifact response must be HTML.");
    }

    [Fact]
    public async Task DownloadFinalizedArtifact_times_out_while_reading_a_stalled_validation_body()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        using var responseBody = new BlockingDocumentReadStream();
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StreamContent(responseBody)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/problem+json") }
                }
            });
        httpClient.Timeout = TimeSpan.FromMilliseconds(100);
        var sut = new DocumentsApiClient(httpClient);
        Func<Task> download = async () => await sut.DownloadFinalizedArtifact(documentId, TestContext.Current.CancellationToken);

        // Act
        await download.ShouldThrowAssignableTo<OperationCanceledException>();

        // Assert
        responseBody.ReadStarted.IsCompletedSuccessfully.ShouldBeTrue();
        responseBody.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task DownloadFinalizedArtifact_preserves_caller_cancellation_while_reading_validation()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var responseBody = new BlockingDocumentReadStream(cts.Cancel);
        using var httpClient = ContractHttpClientTestHelper.CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StreamContent(responseBody)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/problem+json") }
                }
            });
        httpClient.Timeout = TimeSpan.FromMinutes(1);
        var sut = new DocumentsApiClient(httpClient);
        Func<Task> download = async () => await sut.DownloadFinalizedArtifact(documentId, cts.Token);

        // Act
        var exception = await download.ShouldThrowAssignableTo<OperationCanceledException>();

        // Assert
        exception.CancellationToken.ShouldBe(cts.Token);
        responseBody.ReadStarted.IsCompletedSuccessfully.ShouldBeTrue();
        responseBody.IsDisposed.ShouldBeTrue();
    }
}
