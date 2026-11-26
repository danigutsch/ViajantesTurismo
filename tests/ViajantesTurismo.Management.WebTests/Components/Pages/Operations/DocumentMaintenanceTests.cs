using System.Net;
using Microsoft.AspNetCore.Authorization;
using SharedKernel.AspNetCore.Authentication.Bff;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

[TestScope(TestScope.Component)]
public sealed class DocumentMaintenanceTests : BunitContext
{
    [Fact]
    public void Maintenance_history_requires_admin_or_the_dedicated_schedule_controller_role()
    {
        // Arrange
        var type = typeof(DocumentMaintenance);

        // Act
        var policy = type.GetCustomAttributes(typeof(AuthorizeAttribute), false).OfType<AuthorizeAttribute>().ShouldHaveSingleItem();

        // Assert
        policy.Roles.ShouldBe("Admin,OperationsScheduleController");
    }

    [Fact]
    public void Shows_persisted_job_states_and_counts_only_batch_evidence()
    {
        // Arrange
        const string payload = """
            {
              "schedules": [
                {"job":"Drafts","isPaused":true,"fixedDelay":"1.00:00:00","nextDueAtUtc":"2026-01-05T08:00:00Z"},
                {"job":"Audit","isPaused":false,"fixedDelay":"1.00:00:00","nextDueAtUtc":"2026-01-05T08:00:00Z"},
                {"job":"Idempotency","isPaused":null,"fixedDelay":"1.00:00:00"}
              ],
              "draftRuns": [
                { "occurredAtUtc": "2026-01-05T00:00:00Z", "purgedCount": 2, "heldCount": 1, "outcome": "Succeeded" }
              ],
              "auditRuns": [],
              "idempotencyRuns": []
            }
            """;
        using var transport = new TestHttpMessageHandler(HttpResponseFactory.Json(payload, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var component = Render<DocumentMaintenance>();

        // Assert
        component.FindAll("nav [data-schedule-state]").Select(item => item.TextContent).ShouldBe(["Paused", "Scheduled", "Registration pending"]);
        component.Markup.ShouldNotContain("API host", StringComparison.Ordinal);
        component.FindAll("nav a").Select(link => link.GetAttribute("href")).ToArray()
            .ShouldBe(["operations/document-maintenance/drafts", "operations/document-maintenance/audit", "operations/document-maintenance/idempotency"]);
        component.FindAll("tbody td").Select(cell => cell.TextContent).ToArray()
            .ShouldBe(["2026-01-05 00:00:00Z", "Succeeded", "2", "1"]);
        component.FindAll("section p").Select(paragraph => paragraph.TextContent).ToArray()
            .ShouldBe(["No recorded batches.", "No recorded batches."]);
        var request = transport.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/document-maintenance");
    }

    [Fact]
    public void Failed_read_shows_a_generic_operator_message()
    {
        // Arrange
        using var transport = new TestHttpMessageHandler(HttpResponseFactory.Json(
            """{"detail":"synthetic provider detail"}""", HttpStatusCode.ServiceUnavailable));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var component = Render<DocumentMaintenance>();

        // Assert
        component.Find("[role='alert']").TextContent.ShouldBe("Maintenance history is unavailable.");
        component.Markup.ShouldNotContain("synthetic provider detail", StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_token_shows_the_safe_unavailable_message_and_clears_loading()
    {
        // Arrange
        const string privateDetail = "synthetic-private-admin-session";
        using var transport = new RecordingHttpMessageHandler(_ => throw new UserTokenUnavailableException(privateDetail));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://admin.example.test") };
        Services.AddSingleton<IDocumentMaintenanceApiClient>(new DocumentMaintenanceApiClient(client));

        // Act
        var component = Render<DocumentMaintenance>();

        // Assert
        component.Find("h1").TextContent.ShouldBe("Document maintenance");
        component.Find("[role='alert']").TextContent.ShouldBe("Maintenance history is unavailable.");
        component.FindAll("[role='status']").ShouldBeEmpty();
        component.Markup.ShouldNotContain(privateDetail, StringComparison.Ordinal);
        var request = transport.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe("/api/v1/operations/document-maintenance");
    }
}
