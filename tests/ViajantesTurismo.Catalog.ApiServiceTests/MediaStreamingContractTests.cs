using System.Net.Http.Headers;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

/// <summary>
/// Contract tests for the range and disposal behavior of Catalog media delivery.
/// </summary>
[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class MediaStreamingContractTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Media_delivery_ignores_range_requests_and_does_not_advertise_ranges(bool isPublic)
    {
        // Arrange
        await using var scenario = await MediaDeliveryScenario.Create();
        using var client = scenario.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(scenario.Route(isPublic), UriKind.Relative));
        request.Headers.Range = new RangeHeaderValue(2, 5);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.AcceptRanges.ShouldBeEmpty();
        response.Content.Headers.ContentRange.ShouldBeNull();
        content.ShouldBe(MediaDeliveryScenario.Payload);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Media_delivery_disposes_the_opened_object_stream_after_the_response_completes(bool isPublic)
    {
        // Arrange
        await using var scenario = await MediaDeliveryScenario.Create();
        using var client = scenario.Factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(scenario.Route(isPublic), UriKind.Relative), TestContext.Current.CancellationToken);
        _ = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        scenario.ObjectStore.OpenedStreams.ShouldHaveSingleItem().IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Media_delivery_cancels_the_stalled_read_and_disposes_the_stream_when_the_client_disconnects(bool isPublic)
    {
        // Arrange
        await using var scenario = await MediaDeliveryScenario.Create(stallReadsAfterFirstByte: true);
        using var client = scenario.Factory.CreateClient();
        var response = await client.GetAsync(
            new Uri(scenario.Route(isPublic), UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var firstByte = new byte[1];
        var bytesRead = await body.ReadAsync(firstByte, TestContext.Current.CancellationToken);
        bytesRead.ShouldBe(1);

        // Act
        var stream = scenario.ObjectStore.OpenedStreams.ShouldHaveSingleItem();
        await stream.StalledReadStarted.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        response.Dispose();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        while (!stream.IsDisposed)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }

        // Assert
        stream.StalledReadWasCancelled.ShouldBeTrue();
    }
}
