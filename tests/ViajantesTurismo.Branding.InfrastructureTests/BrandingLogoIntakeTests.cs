using ImageMagick;
using SharedKernel.ImageProcessing;
using SharedKernel.MalwareScanning;
using SharedKernel.Results;
using SharedKernel.Testing.MalwareScanning;
using SharedKernel.Testing;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class BrandingLogoIntakeTests
{
    private const string PngType = "image/png";
    private const string ScanRefusal = "Logo did not pass security scanning.";
    private const string AnimationRefusal = "Logo must be a still image; animated images are not accepted.";
    private const string MalformedRefusal = "Logo could not be decoded or exceeds the allowed image dimensions.";

    [Fact]
    public async Task A_noisy_landscape_uses_a_later_webp_variant_and_stays_within_the_logo_budget()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var body = LogoImageBytes.NoisyJpeg();
        using var firstAttempt = new MemoryStream(body);
        var initial = MagickImageProcessor.Process(new ImageProcessingRequest(
            firstAttempt,
            [new ImageVariantRequest("first", ImageOutputFormat.WebP, 512, 90, 512)],
            ImageProcessingLimits.WebDefault), ct).Variants.ShouldHaveSingleItem();
        var scanner = new TestMalwareScanner();
        using var upload = new MemoryStream(body);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(upload, "image/jpeg", ct);

        // Assert
        body.Length.ShouldBeLessThanOrEqualTo(BrandingLogoLimits.MaxUploadBytes);
        initial.Content.Length.ShouldBeGreaterThan(BrandingLogoLimits.MaxEncodedBytes);
        result.IsSuccess.ShouldBeTrue();
        var logo = result.Value;
        logo.Content.Length.ShouldBeInRange(1, BrandingLogoLimits.MaxEncodedBytes);
        logo.ContentType.ShouldBe("image/webp");
        scanner.CallCount.ShouldBe(1);
        using var selected = new MagickImage(logo.Content.ToArray());
        TestContext.Current.TestOutputHelper?.WriteLine(
            $"Input {body.Length} bytes. First 512 px quality 90 {initial.Content.Length} bytes. Selected {selected.Width}x{selected.Height} at {logo.Content.Length} bytes.");
    }

    [Fact]
    public async Task An_animated_png_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.RealAnimatedPng();

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe(AnimationRefusal);
        scanner.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_png_with_a_hostile_chunk_length_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var hostile = LogoImageBytes.PngSignedFiller(64);
        hostile[8] = 0xFF;
        hostile[9] = 0xFF;
        hostile[10] = 0xFF;
        hostile[11] = 0xF0;

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(hostile), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe(MalformedRefusal);
        scanner.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_still_png_is_scanned_once()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        scanner.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_refused_scan_refuses_a_well_formed_image()
    {
        // Arrange
        var scanner = new TestMalwareScanner { Result = MalwareScanResult.Rejected, ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe(ScanRefusal);
    }

    [Fact]
    public async Task The_scanner_receives_the_whole_upload_from_position_zero()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        var scan = scanner.LastScan.ShouldNotBeNull();
        scan.StartPosition.ShouldBe(0);
        scan.Length.ShouldBe(body.Length);
        scan.Content.ToArray().ShouldBe(body);
    }

    [Fact]
    public async Task A_scanner_failure_returns_unavailable_and_disposes_the_upload()
    {
        // Arrange
        var scanner = new TestMalwareScanner { Exception = new InvalidOperationException("down"), ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        await using var body = new TrackingReadStream(LogoImageBytes.Real(MagickFormat.Png)) { MaxBytesPerRead = 4096 };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        scanner.LastScan.ShouldNotBeNull().Stream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_cancelled_scan_propagates_the_cancellation()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        var scanner = new TestMalwareScanner { Exception = new OperationCanceledException(source.Token), ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var intake = LogoIntakeHelpers.Create(scanner);
        var body = LogoImageBytes.Real(MagickFormat.Png);
        await source.CancelAsync();

        // Act
        Func<Task> act = () => intake.Process(new MemoryStream(body), PngType, source.Token).AsTask();

        // Assert
        _ = await act.ShouldThrow<OperationCanceledException>();
    }

    [Fact]
    public async Task A_scanner_timeout_that_the_caller_did_not_request_returns_unavailable()
    {
        // Arrange
        var scanner = new TestMalwareScanner { Exception = new OperationCanceledException("scanner timeout"), ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
    }

    [Fact]
    public async Task A_cancellation_during_the_body_read_stops_reading_before_any_scan()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var bytes = LogoImageBytes.PngSignedFiller(100_000);
        await using var body = new BlockingReadStream(bytes[..1024], bytes[1024..]);
        var processing = LogoIntakeHelpers.Create(scanner).Process(body, PngType, source.Token).AsTask();
        await body.Blocked.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Act
        await source.CancelAsync();
        Func<Task> act = () => processing;

        // Assert
        _ = await act.ShouldThrowAssignableTo<OperationCanceledException>();
        body.BlockedReadCancelled.ShouldBeTrue();
        body.BytesRead.ShouldBe(1024);
        scanner.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_successful_upload_disposes_the_spooled_lease()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        await using var body = new TrackingReadStream(LogoImageBytes.Real(MagickFormat.Png)) { MaxBytesPerRead = 4096 };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        scanner.LastScan.ShouldNotBeNull().Stream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_scan_refusal_disposes_the_spooled_lease()
    {
        // Arrange
        var scanner = new TestMalwareScanner { Result = MalwareScanResult.Rejected, ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        await using var body = new TrackingReadStream(LogoImageBytes.Real(MagickFormat.Png)) { MaxBytesPerRead = 4096 };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        scanner.LastScan.ShouldNotBeNull().Stream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_non_seekable_body_above_the_spool_threshold_is_scanned_intact_and_accepted()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        using var image = new MagickImage(MagickColors.SeaGreen, 400, 400);
        image.AddNoise(NoiseType.Random);
        image.Format = MagickFormat.Png;
        var body = image.ToByteArray();
        body.Length.ShouldBeInRange(256 * 1024 + 1, BrandingLogoLimits.MaxUploadBytes);
        await using var stream = new TrackingReadStream(body) { MaxBytesPerRead = 8192 };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(stream, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        var scan = scanner.LastScan.ShouldNotBeNull();
        scan.Length.ShouldBe(body.Length);
        scan.Content.ToArray().ShouldBe(body);
        scan.Stream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task An_empty_body_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe("Logo content does not match its declared image type.");
        scanner.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_body_of_exactly_the_limit_passes_the_size_gate()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.PngSignedFiller(BrandingLogoLimits.MaxUploadBytes);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new TrackingReadStream(body) { MaxBytesPerRead = 64 * 1024 }, PngType, TestContext.Current.CancellationToken);

        // Assert
        LogoIntakeHelpers.ContentError(result).ShouldBe(MalformedRefusal);
    }

    [Fact]
    public async Task A_body_one_byte_over_the_limit_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.PngSignedFiller(BrandingLogoLimits.MaxUploadBytes + 1);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new TrackingReadStream(body) { MaxBytesPerRead = 64 * 1024 }, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe($"Logo must be between 1 and {BrandingLogoLimits.MaxUploadBytes} bytes.");
        scanner.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("image/png; charset=binary")]
    [InlineData("IMAGE/PNG;q=1")]
    [InlineData("  image/png  ")]
    [InlineData("image/png ; x=y")]
    public async Task Media_type_parameters_are_ignored_and_the_signature_still_decides(string declared)
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), declared, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public async Task A_parameterised_type_does_not_bypass_the_signature_check()
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };
        var jpeg = LogoImageBytes.Real(MagickFormat.Jpeg);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(jpeg), "image/png; charset=binary", TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe("Logo content does not match its declared image type.");
        scanner.CallCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("image/svg+xml")]
    [InlineData("image/png, image/jpeg")]
    [InlineData("image/png;;;")]
    [InlineData("application/octet-stream")]
    public async Task An_undeclared_or_unsupported_type_is_refused(string? declared)
    {
        // Arrange
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.CaptureAndRestorePosition };

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(LogoImageBytes.Real(MagickFormat.Png)), declared, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("ContentType");
        scanner.CallCount.ShouldBe(0);
    }
}
