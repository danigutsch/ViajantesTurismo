using System.Net;
using Microsoft.AspNetCore.Components;
using SharedKernel.Testing;
using SharedKernel.Testing.Http;
using ViajantesTurismo.Management.Web.Components.Pages.Operations;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Operations;

/// <summary>Exercises reverse lookup from a persisted original wake ID in the Gallery viewer.</summary>
[TestScope(TestScope.Component)]
public sealed class GalleryOriginalWakeLookupTests : BunitContext
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Looks_up_an_original_wake_without_requiring_the_tour_or_sequence(bool found)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var wakeId = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{tourId:D}}","sequence":7,"intentId":"{{wakeId:D}}",
             "status":0,"revision":null,"acceptedAt":null,"terminalAt":null}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => found
            ? HttpResponseFactory.Json(body, HttpStatusCode.OK)
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        Services.AddSingleton(TimeProvider.System);
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-original-wake-event").ChangeAsync(new ChangeEventArgs { Value = wakeId.ToString("D") });

        // Act
        await page.Find("#gallery-original-wake-lookup").SubmitAsync();

        // Assert
        var request = transport.Requests.ShouldHaveSingleItem();
        request.Message.Method.ShouldBe(HttpMethod.Get);
        request.Message.RequestUri.ShouldNotBeNull().AbsolutePath.ShouldBe(
            $"/api/v1/catalog/operations/gallery/original-wakes/{wakeId:D}/placement");
        if (found)
        {
            page.Find("dl").TextContent.ShouldContain(tourId.ToString("D"), StringComparison.Ordinal);
            page.Find("a[download]").GetAttribute("href").ShouldBe($"/operations/gallery/{tourId:D}/placements/7/diagnostics");
        }
        else
        {
            page.Find("[role='status']").TextContent.ShouldBe("No Gallery placement matches this original wake event ID.");
            page.FindAll("dl").ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Rejects_a_placement_linked_to_a_different_original_wake()
    {
        // Arrange
        var requestedWake = Guid.CreateVersion7();
        var body = $$"""
            {"catalogTourId":"{{Guid.CreateVersion7():D}}","sequence":7,"intentId":"{{Guid.CreateVersion7():D}}",
             "status":0,"revision":null,"acceptedAt":null,"terminalAt":null}
            """;
        using var transport = new RecordingHttpMessageHandler(_ => HttpResponseFactory.Json(body, HttpStatusCode.OK));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        Services.AddSingleton(TimeProvider.System);
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-original-wake-event").ChangeAsync(new ChangeEventArgs { Value = requestedWake.ToString("D") });

        // Act
        await page.Find("#gallery-original-wake-lookup").SubmitAsync();

        // Assert
        page.Find("[role='alert']").TextContent.ShouldBe("Gallery placement status could not be loaded.");
        page.FindAll("dl").ShouldBeEmpty();
        page.FindAll("a[download]").ShouldBeEmpty();
    }

    [Fact]
    public async Task Rejects_an_empty_original_wake_ID_before_contacting_catalog()
    {
        // Arrange
        using var transport = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://catalog.example.test") };
        Services.AddSingleton<ICatalogGalleryOperationsApiClient>(new CatalogGalleryOperationsApiClient(client));
        Services.AddSingleton(TimeProvider.System);
        var page = Render<GalleryPlacementOperations>();
        await page.Find("#gallery-original-wake-event").ChangeAsync(new ChangeEventArgs { Value = Guid.Empty.ToString("D") });

        // Act
        await page.Find("#gallery-original-wake-lookup").SubmitAsync();

        // Assert
        page.Find("[role='alert']").TextContent.ShouldBe("Enter a valid original Gallery wake event ID.");
        transport.Requests.ShouldBeEmpty();
    }
}
