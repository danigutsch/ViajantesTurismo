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
}
