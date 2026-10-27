using System.Net;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Contracts.Http;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
namespace ViajantesTurismo.Admin.IntegrationTests.Documents;

/// <summary>Arranges bookings and documents for document API tests.</summary>
internal static class DocumentApiTestSetup
{
    /// <summary>Posts to <paramref name="route"/> with an <c>Idempotency-Key</c> header and returns the response.</summary>
    public static async Task<HttpResponseMessage> PostIdempotentCommand(
        HttpClient client,
        Uri route,
        string idempotencyKey,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request, ct);
    }

    /// <summary>Creates a tour, customer and confirmed booking, then generates a contract draft and returns the booking id, client and draft.</summary>
    public static async Task<(Guid BookingId, DocumentsApiClient Client, GetDocumentDto Document)>
        CreateGeneratedDocument(ApiFixture fixture, string scenarioName, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        var tour = await fixture.Client.CreateTestTour(
            $"document-{scenarioName}",
            $"Document {scenarioName}",
            ct);
        var customer = await fixture.Client.CreateTestCustomer("Document", "Lifecycle", ct);
        var booking = await fixture.Client.CreateTestBooking(tour.Id, customer.Id, null, ct);
        using var confirmResponse = await fixture.Client.ConfirmBooking(booking.Id, ct);
        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var client = new DocumentsApiClient(fixture.Client);
        var document = await client.GenerateContractDraft(booking.Id, Guid.CreateVersion7().ToString("N"), ct).RequireSucceededValue();
        return (booking.Id, client, document);
    }
}
