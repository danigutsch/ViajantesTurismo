using ImageMagick;
using PandaBear.MalwareScanning;
using PandaBear.Results;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class BrandingLogoIntakeTests
{
    private const string PngType = "image/png";
    private const string ScanRefusal = "Logo did not pass security scanning.";
    private const string AnimationRefusal = "Logo must be a still image; animated images are not accepted.";

    [Fact]
    public async Task A_refused_scan_returns_before_animation_detection()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner(MalwareScanStatus.Rejected);
        var body = LogoImageBytes.RealAnimatedPng();

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe(ScanRefusal);
        scanner.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task An_unavailable_scanner_is_reported_before_a_hostile_chunk_is_parsed()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner(MalwareScanStatus.Failed);
        var hostile = LogoImageBytes.PngSignedFiller(64);
        hostile[8] = 0xFF;
        hostile[9] = 0xFF;
        hostile[10] = 0xFF;
        hostile[11] = 0xF0;

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(hostile), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        scanner.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task A_clean_scan_is_followed_by_animation_detection()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        var body = LogoImageBytes.RealAnimatedPng();

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe(AnimationRefusal);
        scanner.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task The_scanner_receives_the_whole_upload_from_position_zero()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        var body = LogoImageBytes.Real(MagickFormat.Png);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(body), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        scanner.LastPosition.ShouldBe(0);
        scanner.LastLength.ShouldBe(body.Length);
        scanner.LastContent.ShouldBe(body);
    }

    [Fact]
    public async Task A_scanner_failure_returns_unavailable_and_disposes_the_upload()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner(exception: new InvalidOperationException("down"));
        await using var body = new OneChunkReadStream(LogoImageBytes.Real(MagickFormat.Png));

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        scanner.LastStream.ShouldNotBeNull().CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_cancelled_scan_propagates_the_cancellation()
    {
        // Arrange
        using var source = new CancellationTokenSource();
        var scanner = new RecordingMalwareScanner(exception: new OperationCanceledException(source.Token));
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
        var scanner = new RecordingMalwareScanner(exception: new OperationCanceledException("scanner timeout"));
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
        var scanner = new RecordingMalwareScanner();
        await using var body = new OneChunkReadStream(LogoImageBytes.PngSignedFiller(100_000), 1024, source);

        // Act
        Func<Task> act = () => LogoIntakeHelpers.Create(scanner).Process(body, PngType, source.Token).AsTask();

        // Assert
        _ = await act.ShouldThrow<OperationCanceledException>();
        body.ReadCalls.ShouldBeLessThan(5);
        scanner.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task A_successful_upload_disposes_the_spooled_lease()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        await using var body = new OneChunkReadStream(LogoImageBytes.Real(MagickFormat.Png));

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        scanner.LastStream.ShouldNotBeNull().CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_scan_refusal_disposes_the_spooled_lease()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner(MalwareScanStatus.Rejected);
        await using var body = new OneChunkReadStream(LogoImageBytes.Real(MagickFormat.Png));

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(body, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        scanner.LastStream.ShouldNotBeNull().CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task A_non_seekable_body_above_the_spool_threshold_is_scanned_intact_and_accepted()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        using var image = new MagickImage(MagickColors.SeaGreen, 400, 400);
        image.AddNoise(NoiseType.Random);
        image.Format = MagickFormat.Png;
        var body = image.ToByteArray();
        body.Length.ShouldBeInRange(256 * 1024 + 1, BrandingLogoLimits.MaxUploadBytes);
        await using var stream = new OneChunkReadStream(body, 8192);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(stream, PngType, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeFalse();
        scanner.LastLength.ShouldBe(body.Length);
        scanner.LastContent.ShouldBe(body);
        scanner.LastStream.ShouldNotBeNull().CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task An_empty_body_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe("Logo content does not match its declared image type.");
        scanner.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task A_body_of_exactly_the_limit_passes_the_size_gate()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        var body = LogoImageBytes.PngSignedFiller(BrandingLogoLimits.MaxUploadBytes);

        // Act
        await LogoIntakeHelpers.Create(scanner).Process(new OneChunkReadStream(body, 64 * 1024), PngType, TestContext.Current.CancellationToken);

        // Assert
        scanner.Calls.ShouldBe(1);
        scanner.LastLength.ShouldBe(BrandingLogoLimits.MaxUploadBytes);
    }

    [Fact]
    public async Task A_body_one_byte_over_the_limit_is_refused_before_any_scan()
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
        var body = LogoImageBytes.PngSignedFiller(BrandingLogoLimits.MaxUploadBytes + 1);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new OneChunkReadStream(body, 64 * 1024), PngType, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe($"Logo must be between 1 and {BrandingLogoLimits.MaxUploadBytes} bytes.");
        scanner.Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData("image/png; charset=binary")]
    [InlineData("IMAGE/PNG;q=1")]
    [InlineData("  image/png  ")]
    [InlineData("image/png ; x=y")]
    public async Task Media_type_parameters_are_ignored_and_the_signature_still_decides(string declared)
    {
        // Arrange
        var scanner = new RecordingMalwareScanner();
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
        var scanner = new RecordingMalwareScanner();
        var jpeg = LogoImageBytes.Real(MagickFormat.Jpeg);

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(jpeg), "image/png; charset=binary", TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        LogoIntakeHelpers.ContentError(result).ShouldBe("Logo content does not match its declared image type.");
        scanner.Calls.ShouldBe(0);
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
        var scanner = new RecordingMalwareScanner();

        // Act
        var result = await LogoIntakeHelpers.Create(scanner).Process(new MemoryStream(LogoImageBytes.Real(MagickFormat.Png)), declared, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("ContentType");
        scanner.Calls.ShouldBe(0);
    }
}
