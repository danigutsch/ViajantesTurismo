using Microsoft.Extensions.Options;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.InfrastructureTests;

[Trait(TestTraitNames.ScopeName, "integration")]
public sealed class LocalMediaObjectStoreTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Put_preserves_open_reader_bytes_across_replacements(int replacementCount)
    {
        // Arrange
        using var directory = new TemporaryMediaDirectory();
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));
        await using var originalContent = new MemoryStream([9, 8, 7]);
        _ = await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", originalContent, "image/jpeg", originalContent.Length),
            TestContext.Current.CancellationToken);
        using var originalReader = await store.OpenRead("images/photo.jpg", TestContext.Current.CancellationToken);

        // Act
        for (var index = 0; index < replacementCount; index++)
        {
            await using var replacement = new MemoryStream([(byte)index]);
            _ = await store.Put(
                new MediaObjectWriteRequest("images/photo.jpg", replacement, "image/jpeg", replacement.Length),
                TestContext.Current.CancellationToken);
        }

        using var currentReader = await store.OpenRead("images/photo.jpg", TestContext.Current.CancellationToken);
        using var originalBytes = new MemoryStream();
        using var currentBytes = new MemoryStream();
        await originalReader.Content.CopyToAsync(originalBytes, TestContext.Current.CancellationToken);
        await currentReader.Content.CopyToAsync(currentBytes, TestContext.Current.CancellationToken);
        var keys = await store.ListKeys(string.Empty, TestContext.Current.CancellationToken);
        var files = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories).ToArray();

        // Assert
        originalBytes.ToArray().ShouldBe([9, 8, 7]);
        currentBytes.ToArray().ShouldBe([(byte)(replacementCount - 1)]);
        keys.ShouldBe(["images/photo.jpg"]);
        files.Length.ShouldBe(1);
    }
}