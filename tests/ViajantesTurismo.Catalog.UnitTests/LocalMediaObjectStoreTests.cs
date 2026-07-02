using Microsoft.Extensions.Options;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class LocalMediaObjectStoreTests
{
    [Fact]
    public async Task Put_writes_object_under_configured_media_root()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new MemoryStream([1, 2, 3]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions
        {
            RootPath = directory.Path,
            PublicBaseUri = new Uri("https://cdn.example/media/")
        }));

        // Act
        var result = await store.Put(
            new MediaObjectWriteRequest("images/tour/photo.jpg", content, "image/jpeg", 3, "sha256:test"),
            TestContext.Current.CancellationToken);

        // Assert
        var savedPath = Path.Combine(directory.Path, "images", "tour", "photo.jpg");
        File.Exists(savedPath).ShouldBe(true);
        result.ObjectKey.ShouldBe("images/tour/photo.jpg");
        result.PublicUri.ShouldBe(new Uri("https://cdn.example/media/images/tour/photo.jpg"));
    }

    [Fact]
    public async Task Put_escapes_public_uri_segments()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions
        {
            RootPath = directory.Path,
            PublicBaseUri = new Uri("https://cdn.example/media")
        }));

        // Act
        var result = await store.Put(
            new MediaObjectWriteRequest("tour photos/photo 1.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        result.PublicUri.ShouldBe(new Uri("https://cdn.example/media/tour%20photos/photo%201.jpg"));
    }

    [Fact]
    public async Task Put_builds_relative_public_uri_when_base_uri_is_relative()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions
        {
            RootPath = directory.Path,
            PublicBaseUri = new Uri("/media", UriKind.Relative)
        }));

        // Act
        var result = await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        result.PublicUri.ShouldBe(new Uri("/media/images/photo.jpg", UriKind.Relative));
    }

    [Fact]
    public async Task Delete_removes_object_when_it_exists()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var objectPath = Path.Combine(directory.Path, "images", "tour", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(objectPath) ?? directory.Path);
        await File.WriteAllBytesAsync(objectPath, [1, 2, 3], TestContext.Current.CancellationToken);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        await store.Delete("images/tour/photo.jpg", TestContext.Current.CancellationToken);

        // Assert
        File.Exists(objectPath).ShouldBe(false);
    }

    [Fact]
    public async Task Delete_ignores_missing_object()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        await store.Delete("images/missing.jpg", TestContext.Current.CancellationToken);

        // Assert
        Directory.EnumerateFileSystemEntries(directory.Path).ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_rejects_empty_object_key()
    {
        // Arrange
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.Delete(" ", TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("objectKey");
    }

    [Fact]
    public async Task Put_accepts_valid_keys_when_root_path_has_trailing_separator()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions
        {
            RootPath = directory.Path + Path.DirectorySeparatorChar
        }));

        // Act
        var result = await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        var savedPath = Path.Combine(directory.Path, "images", "photo.jpg");
        File.Exists(savedPath).ShouldBe(true);
        result.ObjectKey.ShouldBe("images/photo.jpg");
    }

    [Fact]
    public async Task Put_rejects_path_traversal_keys()
    {
        // Arrange
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest("../photo.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("objectKey");
    }

    [Fact]
    public async Task Put_rejects_windows_style_path_traversal_keys()
    {
        // Arrange
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest("..\\photo.jpg", content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("objectKey");
    }

    [Fact]
    public async Task Put_rejects_rooted_object_keys()
    {
        // Arrange
        await using var content = new MemoryStream([1]);
        var rootedKey = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/", "photo.jpg");
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest(rootedKey, content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("objectKey");
    }

    [Theory]
    [InlineData("images/../photo.jpg")]
    [InlineData("images/./photo.jpg")]
    [InlineData("images//photo.jpg")]
    public async Task Put_rejects_dot_or_empty_path_segments(string objectKey)
    {
        // Arrange
        await using var content = new MemoryStream([1]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest(objectKey, content, "image/jpeg", 1),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<ArgumentException>();
        exception.ParamName.ShouldBe("objectKey");
    }

    [Fact]
    public async Task CreateUploadUrl_is_not_supported_for_local_storage()
    {
        // Arrange
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));

        // Act
        Func<Task> action = async () => await store.CreateUploadUrl(
            new MediaObjectUploadRequest("images/photo.jpg", "image/jpeg", 1, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        // Assert
        var exception = await action.ShouldThrow<NotSupportedException>();
        exception.Message.ShouldBe("Local media storage does not support direct upload tickets.");
    }

    [Fact]
    public void CreateUploadUrl_rejects_null_request()
    {
        // Arrange
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions()));
        var method = typeof(LocalMediaObjectStore).GetMethod(nameof(LocalMediaObjectStore.CreateUploadUrl)).ShouldNotBeNull();

        // Act
        Action action = () => method.Invoke(store, [null, TestContext.Current.CancellationToken]);

        // Assert
        var exception = action.ShouldThrowInner<ArgumentNullException>();
        exception.ParamName.ShouldBe("request");
    }

}
