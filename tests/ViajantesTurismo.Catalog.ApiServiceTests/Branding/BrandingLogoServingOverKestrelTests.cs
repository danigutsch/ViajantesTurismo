using ImageMagick;
using SharedKernel.Testing;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.UploadWire.Testing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.AreaName, TestTraits.BrandingArea)]
public sealed class BrandingLogoServingOverKestrelTests
{
    [Fact]
    public async Task Successful_anonymous_logo_reads_are_cached_by_address_and_preserve_conditional_requests()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        var logo = BrandingLogo.FromEncodedBytes(BrandingTestImages.Create(MagickFormat.WebP, 8, 8));
        var other = BrandingLogo.FromEncodedBytes(BrandingTestImages.Create(MagickFormat.WebP, 16, 8));
        store.Logos.Add(logo.Sha256, logo);
        store.Logos.Add(other.Sha256, other);
        await using var root = BrandingApiTestHost.CreateAnonymous(store);
        await using var host = KestrelTestHost.StartFromFactory(root);
        using var client = new HttpClient { BaseAddress = host.BaseAddress, Timeout = WireTimeout.Default };
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var first = await BrandingLogoRequests.GetLogo(client, logo.Sha256);
        using var cached = await client.GetAsync(new Uri(BrandingLogoPath.For(logo.Sha256) + "?fixture=1", UriKind.Relative), ct);
        using var conditional = await BrandingLogoRequests.GetLogo(client, logo.Sha256, $"\"{logo.Sha256}\"");
        using var different = await BrandingLogoRequests.GetLogo(client, other.Sha256);
        using var malformed = await BrandingLogoRequests.GetLogo(client, logo.Sha256.ToUpperInvariant());
        var cachedBytes = await cached.Content.ReadAsByteArrayAsync(ct);
        var differentBytes = await different.Content.ReadAsByteArrayAsync(ct);
        var conditionalBytes = await conditional.Content.ReadAsByteArrayAsync(ct);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        cached.StatusCode.ShouldBe(HttpStatusCode.OK);
        cached.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(BrandingLogo.EncodedContentType);
        cached.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe($"\"{logo.Sha256}\"");
        cached.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        cached.Headers.CacheControl.ShouldNotBeNull().ToString().ShouldContain("immutable", StringComparison.Ordinal);
        cachedBytes.SequenceEqual(logo.Content.Span).ShouldBeTrue();
        conditional.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        conditionalBytes.ShouldBeEmpty();
        conditional.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe($"\"{logo.Sha256}\"");
        different.StatusCode.ShouldBe(HttpStatusCode.OK);
        differentBytes.SequenceEqual(other.Content.Span).ShouldBeTrue();
        malformed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        store.LogoReadCount.ShouldBe(2);
    }
}
