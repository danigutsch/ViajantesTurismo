using System.Buffers.Binary;
using ViajantesTurismo.Branding.Infrastructure;
using TestTraits = ViajantesTurismo.Branding.InfrastructureTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Branding.InfrastructureTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class ImageChunkParserTests
{
    [Theory]
    [InlineData("acTL", "Animation")]
    [InlineData("IDAT", "EndOfControlRegion")]
    [InlineData("IEND", "EndOfControlRegion")]
    [InlineData("tEXt", "Other")]
    public void A_png_header_yields_its_role_and_length(string type, string expected)
    {
        // Arrange
        var header = LogoImageBytes.PngChunk(System.Text.Encoding.ASCII.GetBytes(type), new byte[7]).AsSpan(0, 8).ToArray();

        // Act
        var parsed = ImageChunkParser.TryParsePngHeader(header, out var kind, out var length);

        // Assert
        parsed.ShouldBeTrue();
        kind.ToString().ShouldBe(expected);
        length.ShouldBe(7u);
    }

    [Theory]
    [InlineData(0u, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData((uint)int.MaxValue + 1, false)]
    [InlineData(uint.MaxValue, false)]
    public void A_png_length_above_the_format_maximum_is_refused(uint declared, bool accepted)
    {
        // Arrange
        var header = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header, declared);
        "tEXt"u8.CopyTo(header.AsSpan(4));

        // Act
        var parsed = ImageChunkParser.TryParsePngHeader(header, out _, out _);

        // Assert
        parsed.ShouldBe(accepted);
    }

    [Fact]
    public void A_truncated_png_header_is_refused_at_every_length()
    {
        // Arrange
        var header = new byte[8];

        for (var length = 0; length < header.Length; length++)
        {
            // Act
            var parsed = ImageChunkParser.TryParsePngHeader(header.AsSpan(0, length), out _, out _);

            // Assert
            parsed.ShouldBeFalse($"length {length}");
        }
    }

    [Theory]
    [InlineData(8L, 0u, 100L, true, 20L)]
    [InlineData(8L, 10u, 30L, true, 30L)]
    [InlineData(8L, 10u, 29L, false, 29L)]
    [InlineData(8L, uint.MaxValue, 1_048_576L, false, 1_048_576L)]
    [InlineData(long.MaxValue - 16, uint.MaxValue, long.MaxValue, false, long.MaxValue)]
    public void The_next_png_position_is_computed_in_long_and_overrun_is_reported(long position, uint length, long streamLength, bool fits, long expectedNext)
    {
        // Act
        var result = ImageChunkParser.TryGetNextPngPosition(position, length, streamLength, out var next);

        // Assert
        result.ShouldBe(fits);
        next.ShouldBe(expectedNext);
    }

    [Theory]
    [InlineData("ANIM", "Animation")]
    [InlineData("ANMF", "Animation")]
    [InlineData("VP8X", "ExtendedHeader")]
    [InlineData("VP8 ", "Other")]
    public void A_webp_header_yields_its_role_and_size(string type, string expected)
    {
        // Arrange
        var header = LogoImageBytes.WebPChunk(System.Text.Encoding.ASCII.GetBytes(type), new byte[6]).AsSpan(0, 8).ToArray();

        // Act
        var parsed = ImageChunkParser.TryParseWebPHeader(header, out var kind, out var size);

        // Assert
        parsed.ShouldBeTrue();
        kind.ToString().ShouldBe(expected);
        size.ShouldBe(6u);
    }

    [Fact]
    public void A_truncated_webp_header_is_refused_at_every_length()
    {
        // Arrange
        var header = new byte[8];

        for (var length = 0; length < header.Length; length++)
        {
            // Act
            var parsed = ImageChunkParser.TryParseWebPHeader(header.AsSpan(0, length), out _, out _);

            // Assert
            parsed.ShouldBeFalse($"length {length}");
        }
    }

    [Theory]
    [InlineData(0u, 1000L, 8L)]
    [InlineData(100u, 1000L, 108L)]
    [InlineData(100u, 50L, 50L)]
    [InlineData(uint.MaxValue, 1000L, 1000L)]
    public void The_webp_end_is_bounded_by_the_stream_length(uint riffSize, long streamLength, long expectedEnd)
    {
        // Arrange
        var field = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(field, riffSize);

        // Act
        var result = ImageChunkParser.TryGetWebPEnd(field, streamLength, out var end);

        // Assert
        result.ShouldBeTrue();
        end.ShouldBe(expectedEnd);
    }

    [Fact]
    public void A_truncated_riff_size_field_is_refused()
    {
        // Act
        var result = ImageChunkParser.TryGetWebPEnd(new byte[3], 1000, out _);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(12L, 0u, 20L, true, 20L)]
    [InlineData(12L, 11u, 31L, true, 32L)]
    [InlineData(12L, 11u, 30L, false, 30L)]
    [InlineData(12L, uint.MaxValue, 1_048_576L, false, 1_048_576L)]
    [InlineData(long.MaxValue - 16, uint.MaxValue, long.MaxValue, false, long.MaxValue)]
    public void The_next_webp_position_pads_odd_payloads_and_reports_overrun(long position, uint size, long end, bool fits, long expectedNext)
    {
        // Act
        var result = ImageChunkParser.TryGetNextWebPPosition(position, size, end, out var next);

        // Assert
        result.ShouldBe(fits);
        next.ShouldBe(expectedNext);
    }
}
