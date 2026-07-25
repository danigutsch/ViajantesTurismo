using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
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
    public async Task OpenRead_returns_stored_object_content()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var objectPath = Path.Combine(directory.Path, "images", "tour", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(objectPath) ?? directory.Path);
        await File.WriteAllBytesAsync(objectPath, [1, 2, 3], TestContext.Current.CancellationToken);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        using var result = await store.OpenRead("images/tour/photo.jpg", TestContext.Current.CancellationToken);

        // Assert
        result.ObjectKey.ShouldBe("images/tour/photo.jpg");
        result.ContentType.ShouldBe("image/jpeg");
        result.Length.ShouldBe(3);
        using var content = new MemoryStream();
        await result.Content.CopyToAsync(content, TestContext.Current.CancellationToken);
        content.ToArray().ShouldBe([1, 2, 3]);
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
    public async Task Put_failure_does_not_publish_a_new_object_or_leave_temporary_files()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new PrefixThenThrowStream([1, 2], new IOException("synthetic copy failure"));
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 3),
            TestContext.Current.CancellationToken);
        _ = await action.ShouldThrow<IOException>();
        var keys = await store.ListKeys(string.Empty, TestContext.Current.CancellationToken);
        var storedFiles = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories).ToArray();

        // Assert
        File.Exists(Path.Combine(directory.Path, "images", "photo.jpg")).ShouldBeFalse();
        keys.ShouldBeEmpty();
        storedFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Put_failure_preserves_existing_object_bytes_and_last_modified_time()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var path = Path.Combine(directory.Path, "images", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(path).ShouldNotBeNull());
        await File.WriteAllBytesAsync(path, [9, 8, 7], TestContext.Current.CancellationToken);
        var lastModified = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, lastModified);
        await using var content = new PrefixThenThrowStream([1, 2], new IOException("synthetic copy failure"));
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        Func<Task> action = async () => await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 3),
            TestContext.Current.CancellationToken);
        _ = await action.ShouldThrow<IOException>();
        var persistedBytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);

        // Assert
        persistedBytes.ShouldBe([9, 8, 7]);
        File.GetLastWriteTimeUtc(path).ShouldBe(lastModified);
    }

    [Fact]
    public async Task Put_does_not_publish_a_new_object_until_copy_completes()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new BlockingReadStream([1, 2], [3, 4]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));
        var path = Path.Combine(directory.Path, "images", "photo.jpg");

        // Act
        var put = store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 4),
            TestContext.Current.CancellationToken).AsTask();
        await content.WaitUntilBlocked(TestContext.Current.CancellationToken);
        var existsWhileBlocked = File.Exists(path);
        var keysWhileBlocked = await store.ListKeys(string.Empty, TestContext.Current.CancellationToken);
        var objectsWhileBlocked = await store.ListObjects(string.Empty, TestContext.Current.CancellationToken);
        content.Release();
        _ = await put;
        var persistedBytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);

        // Assert
        existsWhileBlocked.ShouldBeFalse();
        keysWhileBlocked.ShouldBeEmpty();
        objectsWhileBlocked.ShouldBeEmpty();
        persistedBytes.ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public async Task Put_cancellation_does_not_publish_or_inventory_a_temporary_object()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        await using var content = new BlockingReadStream([1, 2], [3, 4]);
        using var cancellation = new CancellationTokenSource();
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));
        var put = store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 4),
            cancellation.Token).AsTask();
        await content.WaitUntilBlocked(TestContext.Current.CancellationToken);

        // Act
        await cancellation.CancelAsync();
        Func<Task> action = () => put;
        _ = await action.ShouldThrowAssignableTo<OperationCanceledException>();
        var keys = await store.ListKeys(string.Empty, TestContext.Current.CancellationToken);
        var objects = await store.ListObjects(string.Empty, TestContext.Current.CancellationToken);
        var storedFiles = Directory.EnumerateFiles(directory.Path, "*", SearchOption.AllDirectories).ToArray();

        // Assert
        File.Exists(Path.Combine(directory.Path, "images", "photo.jpg")).ShouldBeFalse();
        keys.ShouldBeEmpty();
        objects.ShouldBeEmpty();
        storedFiles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Put_preserves_the_existing_object_until_replacement_completes()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var path = Path.Combine(directory.Path, "images", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(path).ShouldNotBeNull());
        await File.WriteAllBytesAsync(path, [9, 8, 7], TestContext.Current.CancellationToken);
        await using var content = new BlockingReadStream([1, 2], [3, 4]);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        var put = store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", content, "image/jpeg", 4),
            TestContext.Current.CancellationToken).AsTask();
        await content.WaitUntilBlocked(TestContext.Current.CancellationToken);
        var bytesWhileBlocked = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        content.Release();
        _ = await put;
        var persistedBytes = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);

        // Assert
        bytesWhileBlocked.ShouldBe([9, 8, 7]);
        persistedBytes.ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public async Task Put_replaces_an_object_while_an_existing_reader_is_open()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));
        await using var originalContent = new MemoryStream([9, 8, 7]);
        _ = await store.Put(
            new MediaObjectWriteRequest("images/photo.jpg", originalContent, "image/jpeg", originalContent.Length),
            TestContext.Current.CancellationToken);
        using var existingReader = await store.OpenRead(
            "images/photo.jpg",
            TestContext.Current.CancellationToken);
        await using var replacementContent = new MemoryStream([1, 2, 3]);

        // Act
        _ = await store.Put(
            new MediaObjectWriteRequest(
                "images/photo.jpg",
                replacementContent,
                "image/jpeg",
                replacementContent.Length),
            TestContext.Current.CancellationToken);
        using var replacementReader = await store.OpenRead(
            "images/photo.jpg",
            TestContext.Current.CancellationToken);
        using var replacementBytes = new MemoryStream();
        await replacementReader.Content.CopyToAsync(replacementBytes, TestContext.Current.CancellationToken);

        // Assert
        replacementBytes.ToArray().ShouldBe([1, 2, 3]);
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
    public async Task Exists_returns_whether_object_is_stored()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var objectPath = Path.Combine(directory.Path, "media", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(objectPath) ?? directory.Path);
        await File.WriteAllBytesAsync(objectPath, [1], TestContext.Current.CancellationToken);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        var existing = await store.Exists("media/photo.jpg", TestContext.Current.CancellationToken);
        var missing = await store.Exists("media/missing.jpg", TestContext.Current.CancellationToken);

        // Assert
        existing.ShouldBeTrue();
        missing.ShouldBeFalse();
    }

    [Fact]
    public async Task ListKeys_returns_keys_under_prefix()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var mediaPath = Path.Combine(directory.Path, "media", "photo.jpg");
        var otherPath = Path.Combine(directory.Path, "other", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(mediaPath) ?? directory.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(otherPath) ?? directory.Path);
        await File.WriteAllBytesAsync(mediaPath, [1], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(otherPath, [2], TestContext.Current.CancellationToken);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        var keys = await store.ListKeys("media/", TestContext.Current.CancellationToken);

        // Assert
        keys.ShouldContain("media/photo.jpg");
        keys.ShouldNotContain("other/photo.jpg");
    }

    [Fact]
    public async Task ListObjects_returns_keys_and_last_modified_times_under_prefix()
    {
        // Arrange
        using var directory = TemporaryMediaDirectory.Create();
        var mediaPath = Path.Combine(directory.Path, "media", "photo.jpg");
        var otherPath = Path.Combine(directory.Path, "other", "photo.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(mediaPath) ?? directory.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(otherPath) ?? directory.Path);
        await File.WriteAllBytesAsync(mediaPath, [1], TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(otherPath, [2], TestContext.Current.CancellationToken);
        var lastModifiedAt = new DateTimeOffset(2026, 7, 7, 10, 0, 0, TimeSpan.Zero);
        File.SetLastWriteTimeUtc(mediaPath, lastModifiedAt.UtcDateTime);
        var store = new LocalMediaObjectStore(Options.Create(new LocalMediaObjectStorageOptions { RootPath = directory.Path }));

        // Act
        var objects = await store.ListObjects("media/", TestContext.Current.CancellationToken);

        // Assert
        var item = objects.ShouldHaveSingleItem();
        item.ObjectKey.ShouldBe("media/photo.jpg");
        item.LastModifiedAt.ShouldBe(lastModifiedAt);
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
