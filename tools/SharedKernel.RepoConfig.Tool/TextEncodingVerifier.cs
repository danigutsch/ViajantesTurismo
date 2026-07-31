using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace SharedKernel.RepoConfig.Tool;

internal static class TextEncodingVerifier
{
    private const int BufferSize = 81_920;
    private const int MaxBlobBytes = 64 * 1024 * 1024;
    private const int MaxGitOutputBytes = 64 * 1024 * 1024;
    private const int MaxHeaderBytes = 512;
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(2);
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static async Task<IReadOnlyList<RepoConfigIssue>> Verify(string rootPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        var canonicalRoot = Path.GetFullPath(rootPath);

        var discovery = GitInvocation.CreateDiscovery(canonicalRoot);
        var discoveryResult = await DiscoverRepository(discovery, cancellationToken).ConfigureAwait(false);
        var sourceRepository = discoveryResult.Repository;
        if (sourceRepository is null)
        {
            return [new RepoConfigIssue(
                ".",
                discoveryResult.IndexFailure
                    ? "Git index inspection failed."
                    : "Git attribute isolation failed.")];
        }

        string? isolationRoot = null;
        try
        {
            isolationRoot = CreateIsolationRoot();
            var isolated = await CreateIsolatedRepository(
                discovery,
                sourceRepository,
                isolationRoot,
                cancellationToken).ConfigureAwait(false);
            if (isolated is null)
            {
                return [new RepoConfigIssue(".", "Git attribute isolation failed.")];
            }

            return await VerifyIsolatedRepository(isolated, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            return [new RepoConfigIssue(".", "Git attribute isolation failed.")];
        }
        finally
        {
            DeleteIsolationRoot(isolationRoot);
        }
    }

    private static async Task<IReadOnlyList<RepoConfigIssue>> VerifyIsolatedRepository(
        GitInvocation isolated,
        CancellationToken cancellationToken)
    {
        var indexResult = await RunGitCapture(
            isolated,
            ["ls-files", "--stage", "-z"],
            standardInput: null,
            MaxGitOutputBytes,
            cancellationToken).ConfigureAwait(false);
        if (indexResult is null || !TryParseIndex(indexResult, out var regularEntries))
        {
            return [new RepoConfigIssue(".", "Git index inspection failed.")];
        }

        if (regularEntries.Count == 0)
        {
            return [];
        }

        var attributeInput = BuildPathInput(regularEntries);
        var attributeResult = await RunGitCapture(
            isolated,
            ["check-attr", "--cached", "-z", "--stdin", "binary"],
            attributeInput,
            MaxGitOutputBytes,
            cancellationToken).ConfigureAwait(false);
        if (attributeResult is null
            || !TrySelectTextEntries(regularEntries, attributeResult, out var textEntries))
        {
            return [new RepoConfigIssue(".", "Git attribute inspection failed.")];
        }

        return textEntries.Count == 0
            ? []
            : await VerifyBlobs(isolated, textEntries, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(SourceRepository? Repository, bool IndexFailure)> DiscoverRepository(
        GitInvocation discovery,
        CancellationToken cancellationToken)
    {
        var indexPath = await ReadGitValue(
            discovery,
            ["rev-parse", "--path-format=absolute", "--git-path", "index"],
            allowEmpty: false,
            cancellationToken).ConfigureAwait(false);
        var objectDirectory = await ReadGitValue(
            discovery,
            ["rev-parse", "--path-format=absolute", "--git-path", "objects"],
            allowEmpty: false,
            cancellationToken).ConfigureAwait(false);
        var objectFormat = await ReadGitValue(
            discovery,
            ["rev-parse", "--show-object-format=storage"],
            allowEmpty: false,
            cancellationToken).ConfigureAwait(false);

        if (indexPath is null
            || objectDirectory is null
            || objectFormat is not ("sha1" or "sha256")
            || !Path.IsPathFullyQualified(indexPath)
            || !Path.IsPathFullyQualified(objectDirectory)
            || !Directory.Exists(objectDirectory))
        {
            return (null, IndexFailure: false);
        }

        var sharedIndexPath = await ReadGitValue(
            discovery,
            ["rev-parse", "--path-format=absolute", "--shared-index-path"],
            allowEmpty: true,
            cancellationToken).ConfigureAwait(false);
        if (sharedIndexPath is null
            || (sharedIndexPath.Length > 0 && !Path.IsPathFullyQualified(sharedIndexPath)))
        {
            return (null, IndexFailure: true);
        }

        return (new SourceRepository(
            Path.GetFullPath(indexPath),
            sharedIndexPath.Length == 0 ? null : Path.GetFullPath(sharedIndexPath),
            Path.GetFullPath(objectDirectory),
            objectFormat),
            IndexFailure: false);
    }

    private static async Task<GitInvocation?> CreateIsolatedRepository(
        GitInvocation discovery,
        SourceRepository sourceRepository,
        string isolationRoot,
        CancellationToken cancellationToken)
    {
        var gitDirectory = Path.Combine(isolationRoot, "repository.git");
        var initResult = await RunGitCapture(
            discovery,
            ["init", "--bare", "--quiet", $"--object-format={sourceRepository.ObjectFormat}", gitDirectory],
            standardInput: null,
            maximumOutputBytes: 4_096,
            cancellationToken).ConfigureAwait(false);
        if (initResult is null)
        {
            return null;
        }

        var indexPath = Path.Combine(gitDirectory, "index");
        var indexCopied = await CopyFileIfPresent(
            sourceRepository.IndexPath,
            indexPath,
            cancellationToken).ConfigureAwait(false);
        if (!indexCopied && sourceRepository.SharedIndexPath is not null)
        {
            return null;
        }

        if (sourceRepository.SharedIndexPath is not null)
        {
            var sharedIndexName = Path.GetFileName(sourceRepository.SharedIndexPath);
            if (!sharedIndexName.StartsWith("sharedindex.", StringComparison.Ordinal)
                || !await CopyFileIfPresent(
                    sourceRepository.SharedIndexPath,
                    Path.Combine(gitDirectory, sharedIndexName),
                    cancellationToken).ConfigureAwait(false))
            {
                return null;
            }
        }

        var globalConfigPath = Path.Combine(isolationRoot, "global.gitconfig");
        var globalAttributesPath = Path.Combine(isolationRoot, "global.attributes");
        var infoAttributesPath = Path.Combine(gitDirectory, "info", "attributes");
        await WriteEmptyPrivateFile(globalConfigPath, cancellationToken).ConfigureAwait(false);
        await WriteEmptyPrivateFile(globalAttributesPath, cancellationToken).ConfigureAwait(false);
        await WriteEmptyPrivateFile(infoAttributesPath, cancellationToken).ConfigureAwait(false);

        return new GitInvocation(
            discovery.RootPath,
            globalAttributesPath,
            globalConfigPath,
            gitDirectory,
            indexPath,
            sourceRepository.ObjectDirectory);
    }

    private static async Task<string?> ReadGitValue(
        GitInvocation invocation,
        IReadOnlyList<string> arguments,
        bool allowEmpty,
        CancellationToken cancellationToken)
    {
        var result = await RunGitCapture(
            invocation,
            arguments,
            standardInput: null,
            maximumOutputBytes: 4_096,
            cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        string value;
        try
        {
            value = StrictUtf8.GetString(result).TrimEnd('\r', '\n');
        }
        catch (DecoderFallbackException)
        {
            return null;
        }

        if (value.IndexOfAny(['\r', '\n', '\0']) >= 0 || (!allowEmpty && value.Length == 0))
        {
            return null;
        }

        return value;
    }

    private static string CreateIsolationRoot()
    {
        var path = Directory.CreateTempSubdirectory("sharedkernel-repo-text-encoding-").FullName;
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return path;
    }

    private static async Task<bool> CopyFileIfPresent(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using FileStream source = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using FileStream destination = new(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await source.CopyToAsync(destination, BufferSize, cancellationToken).ConfigureAwait(false);
            SetPrivateFileMode(destinationPath);
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    private static async Task WriteEmptyPrivateFile(string path, CancellationToken cancellationToken)
    {
        await File.WriteAllBytesAsync(path, [], cancellationToken).ConfigureAwait(false);
        SetPrivateFileMode(path);
    }

    private static void SetPrivateFileMode(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static void DeleteIsolationRoot(string? isolationRoot)
    {
        if (isolationRoot is null)
        {
            return;
        }

        try
        {
            Directory.Delete(isolationRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException)
        {
            // The verification result must not expose a private temporary path during best-effort cleanup.
        }
    }

    private static bool TryParseIndex(byte[] output, out List<IndexEntry> regularEntries)
    {
        regularEntries = [];
        HashSet<string> paths = new(StringComparer.Ordinal);
        var offset = 0;
        while (offset < output.Length)
        {
            var terminator = Array.IndexOf(output, (byte)0, offset);
            if (terminator < 0 || terminator == offset)
            {
                return false;
            }

            var record = output.AsSpan(offset, terminator - offset);
            var separator = record.IndexOf((byte)'\t');
            if (separator <= 0 || separator == record.Length - 1)
            {
                return false;
            }

            if (!TryDecodeAscii(record[..separator], out var metadata))
            {
                return false;
            }

            var metadataParts = metadata.Split(' ');
            if (metadataParts is not [var mode, var objectId, "0"]
                || !IsObjectId(objectId))
            {
                return false;
            }

            var rawPath = record[(separator + 1)..].ToArray();
            string path;
            try
            {
                path = StrictUtf8.GetString(rawPath);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            if (string.IsNullOrEmpty(path) || !paths.Add(path))
            {
                return false;
            }

            switch (mode)
            {
                case "100644":
                case "100755":
                    regularEntries.Add(new IndexEntry(path, rawPath, objectId));
                    break;

                case "120000":
                case "160000":
                    break;

                default:
                    return false;
            }

            offset = terminator + 1;
        }

        return true;
    }

    private static byte[] BuildPathInput(IReadOnlyCollection<IndexEntry> entries)
    {
        using MemoryStream input = new();
        foreach (var entry in entries)
        {
            input.Write(entry.RawPath);
            input.WriteByte(0);
        }

        return input.ToArray();
    }

    private static bool TrySelectTextEntries(
        IReadOnlyList<IndexEntry> entries,
        byte[] output,
        out List<IndexEntry> textEntries)
    {
        textEntries = [];
        HashSet<string> returnedPaths = new(StringComparer.Ordinal);
        var offset = 0;
        foreach (var entry in entries)
        {
            if (!TryReadNullField(output, ref offset, out var rawPath)
                || !rawPath.SequenceEqual(entry.RawPath)
                || !returnedPaths.Add(entry.Path)
                || !TryReadNullField(output, ref offset, out var attribute)
                || !attribute.SequenceEqual("binary"u8)
                || !TryReadNullField(output, ref offset, out var value))
            {
                return false;
            }

            if (!value.SequenceEqual("set"u8))
            {
                textEntries.Add(entry);
            }
        }

        return offset == output.Length;
    }

    private static bool TryReadNullField(byte[] output, ref int offset, out ReadOnlySpan<byte> field)
    {
        if (offset >= output.Length)
        {
            field = default;
            return false;
        }

        var terminator = Array.IndexOf(output, (byte)0, offset);
        if (terminator < 0)
        {
            field = default;
            return false;
        }

        field = output.AsSpan(offset, terminator - offset);
        offset = terminator + 1;
        return true;
    }

    private static async Task<IReadOnlyList<RepoConfigIssue>> VerifyBlobs(
        GitInvocation isolated,
        IReadOnlyList<IndexEntry> entries,
        CancellationToken cancellationToken)
    {
        Process process;
        try
        {
            process = StartGit(isolated, ["cat-file", "--batch"]);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            return [new RepoConfigIssue(".", "Git blob inspection failed.")];
        }

        Task errorDrain = Task.CompletedTask;
        Task processExit = Task.CompletedTask;
        try
        {
            errorDrain = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
            processExit = process.WaitForExitAsync(cancellationToken);
            var byteBuffer = new byte[BufferSize];
            var charBuffer = new char[StrictUtf8.GetMaxCharCount(BufferSize)];
            List<RepoConfigIssue> issues = [];
            foreach (var entry in entries)
            {
                await WriteBlobRequest(
                    process.StandardInput.BaseStream,
                    entry.ObjectId,
                    cancellationToken).ConfigureAwait(false);
                var header = await ReadLine(process.StandardOutput.BaseStream, MaxHeaderBytes, cancellationToken).ConfigureAwait(false);
                if (header is null
                    || !TryParseBlobHeader(header, entry.ObjectId, out var blobSize))
                {
                    return [new RepoConfigIssue(".", "Git blob inspection failed.")];
                }

                if (blobSize > MaxBlobBytes)
                {
                    return [new RepoConfigIssue(entry.Path, "Blob exceeds the 64 MiB text scan limit.")];
                }

                var contentResult = await InspectBlob(
                    process.StandardOutput.BaseStream,
                    blobSize,
                    byteBuffer,
                    charBuffer,
                    cancellationToken).ConfigureAwait(false);
                if (contentResult == BlobContentResult.Malformed)
                {
                    return [new RepoConfigIssue(".", "Git blob inspection failed.")];
                }

                if (contentResult == BlobContentResult.ContainsNul)
                {
                    issues.Add(new RepoConfigIssue(entry.Path, "Contains NUL byte."));
                }
                else if (contentResult == BlobContentResult.InvalidUtf8)
                {
                    issues.Add(new RepoConfigIssue(entry.Path, "Is not valid UTF-8."));
                }
            }

            await CloseStandardInput(process).ConfigureAwait(false);
            if (!await WaitForCompletion(
                Task.WhenAll(processExit, errorDrain),
                cancellationToken).ConfigureAwait(false))
            {
                return [new RepoConfigIssue(".", "Git blob inspection failed.")];
            }

            return process.ExitCode == 0
                ? issues
                : [new RepoConfigIssue(".", "Git blob inspection failed.")];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or UnauthorizedAccessException
            or AggregateException)
        {
            return [new RepoConfigIssue(".", "Git blob inspection failed.")];
        }
        finally
        {
            await CloseStandardInput(process).ConfigureAwait(false);
            await StopProcess(process).ConfigureAwait(false);
            await ObserveCleanup(
                Task.WhenAll(errorDrain, processExit),
                ProcessExitTimeout).ConfigureAwait(false);

            process.Dispose();
        }
    }

    private static async Task WriteBlobRequest(
        Stream standardInput,
        string objectId,
        CancellationToken cancellationToken)
    {
        var request = Encoding.ASCII.GetBytes(objectId + "\n");
        await standardInput.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await standardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CloseStandardInput(Process process)
    {
        try
        {
            await process.StandardInput.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or ObjectDisposedException
            or OperationCanceledException
            or NotSupportedException)
        {
            // Closing stdin is best effort before bounded process termination.
        }
    }

    private static async Task<BlobContentResult> InspectBlob(
        Stream output,
        long blobSize,
        byte[] byteBuffer,
        char[] charBuffer,
        CancellationToken cancellationToken)
    {
        var decoder = StrictUtf8.GetDecoder();
        var containsNul = false;
        var invalidUtf8 = false;
        var remaining = blobSize;
        while (remaining > 0)
        {
            var requested = (int)Math.Min(byteBuffer.Length, remaining);
            var read = await output.ReadAsync(byteBuffer.AsMemory(0, requested), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return BlobContentResult.Malformed;
            }

            containsNul |= byteBuffer.AsSpan(0, read).Contains((byte)0);
            remaining -= read;
            if (!invalidUtf8)
            {
                try
                {
                    decoder.Convert(
                        byteBuffer,
                        0,
                        read,
                        charBuffer,
                        0,
                        charBuffer.Length,
                        flush: remaining == 0,
                        out _,
                        out _,
                        out _);
                }
                catch (DecoderFallbackException)
                {
                    invalidUtf8 = true;
                }
            }
        }

        if (blobSize == 0)
        {
            try
            {
                decoder.Convert([], 0, 0, charBuffer, 0, charBuffer.Length, flush: true, out _, out _, out _);
            }
            catch (DecoderFallbackException)
            {
                invalidUtf8 = true;
            }
        }

        var delimiter = new byte[1];
        if (await output.ReadAsync(delimiter, cancellationToken).ConfigureAwait(false) != 1 || delimiter[0] != (byte)'\n')
        {
            return BlobContentResult.Malformed;
        }

        if (containsNul)
        {
            return BlobContentResult.ContainsNul;
        }

        return invalidUtf8 ? BlobContentResult.InvalidUtf8 : BlobContentResult.Valid;
    }

    private static bool TryParseBlobHeader(byte[] header, string expectedObjectId, out long blobSize)
    {
        blobSize = 0;
        if (!TryDecodeAscii(header, out var value))
        {
            return false;
        }

        var fields = value.Split(' ');
        return fields is [var objectId, "blob", var size]
            && string.Equals(objectId, expectedObjectId, StringComparison.Ordinal)
            && long.TryParse(size, NumberStyles.None, CultureInfo.InvariantCulture, out blobSize)
            && blobSize >= 0;
    }

    private static async Task<byte[]?> ReadLine(Stream stream, int maximumBytes, CancellationToken cancellationToken)
    {
        using MemoryStream line = new();
        var singleByte = new byte[1];
        while (line.Length <= maximumBytes)
        {
            var read = await stream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return null;
            }

            if (singleByte[0] == (byte)'\n')
            {
                return line.ToArray();
            }

            line.WriteByte(singleByte[0]);
        }

        return null;
    }

    private static async Task<byte[]?> RunGitCapture(
        GitInvocation invocation,
        IReadOnlyList<string> arguments,
        byte[]? standardInput,
        int maximumOutputBytes,
        CancellationToken cancellationToken)
    {
        Process process;
        try
        {
            process = StartGit(invocation, arguments);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or UnauthorizedAccessException)
        {
            return null;
        }

        var inputWriter = WriteStandardInput(process.StandardInput.BaseStream, standardInput, cancellationToken);
        var outputReader = ReadBounded(process.StandardOutput.BaseStream, maximumOutputBytes, cancellationToken);
        var errorDrain = process.StandardError.BaseStream.CopyToAsync(Stream.Null, cancellationToken);
        var processExit = process.WaitForExitAsync(cancellationToken);
        var completedNormally = false;
        try
        {
            var firstCompleted = await Task.WhenAny(inputWriter, outputReader).ConfigureAwait(false);
            if (ReferenceEquals(firstCompleted, inputWriter))
            {
                await inputWriter.ConfigureAwait(false);
            }

            var capturedOutput = await outputReader.ConfigureAwait(false);
            if (capturedOutput is null)
            {
                return null;
            }

            await inputWriter.ConfigureAwait(false);
            if (!await WaitForCompletion(
                Task.WhenAll(processExit, errorDrain),
                cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            completedNormally = true;
            return process.ExitCode == 0
                ? capturedOutput
                : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or UnauthorizedAccessException
            or AggregateException)
        {
            return null;
        }
        finally
        {
            if (!completedNormally)
            {
                await StopProcess(process).ConfigureAwait(false);
            }

            await ObserveCleanup(
                Task.WhenAll(inputWriter, outputReader, errorDrain, processExit),
                ProcessExitTimeout).ConfigureAwait(false);

            process.Dispose();
        }
    }

    private static async Task WriteStandardInput(
        Stream standardInput,
        byte[]? content,
        CancellationToken cancellationToken)
    {
        try
        {
            if (content is not null)
            {
                await standardInput.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await standardInput.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Process StartGit(GitInvocation invocation, IReadOnlyList<string> arguments)
    {
        var gitExecutable = ExecutableResolver.Resolve(
            "git",
            Environment.GetEnvironmentVariable("PATH"),
            OperatingSystem.IsWindows())
            ?? throw new InvalidOperationException("Could not resolve git to an absolute executable path.");
        ProcessStartInfo startInfo = new(gitExecutable)
        {
            WorkingDirectory = invocation.RootPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var variable in startInfo.Environment.Keys.Where(
            key => key.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            startInfo.Environment.Remove(variable);
        }

        startInfo.Environment["GIT_ATTR_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_CONFIG_GLOBAL"] = invocation.GlobalConfigFile;
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_NO_REPLACE_OBJECTS"] = "1";
        if (invocation.GitDirectory is not null)
        {
            startInfo.Environment["GIT_DIR"] = invocation.GitDirectory;
            startInfo.Environment["GIT_INDEX_FILE"] = invocation.IndexFile;
            startInfo.Environment["GIT_OBJECT_DIRECTORY"] = invocation.ObjectDirectory;
        }

        startInfo.ArgumentList.Add("--no-replace-objects");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"core.attributesFile={invocation.AttributesFile}");
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(invocation.RootPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Git process did not start.");
    }

    private static async Task<byte[]?> ReadBounded(Stream stream, int maximumBytes, CancellationToken cancellationToken)
    {
        using MemoryStream output = new();
        var buffer = new byte[BufferSize];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return output.ToArray();
            }

            if (output.Length + read > maximumBytes)
            {
                return null;
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<bool> WaitForCompletion(Task completion, CancellationToken cancellationToken)
    {
        try
        {
            await completion.WaitAsync(ProcessExitTimeout, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    internal static async Task ObserveCleanup(Task cleanup, TimeSpan timeout)
    {
        try
        {
            await cleanup.WaitAsync(timeout).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _ = cleanup.ContinueWith(
                static task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
        catch (Exception exception) when (exception is IOException
            or InvalidOperationException
            or ObjectDisposedException
            or OperationCanceledException
            or AggregateException
            or NotSupportedException
            or Win32Exception
            or UnauthorizedAccessException)
        {
            // Cleanup observation must not replace the sanitized result or caller cancellation.
        }
    }

    private static async Task StopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                using CancellationTokenSource timeout = new(ProcessExitTimeout);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or Win32Exception
            or NotSupportedException
            or OperationCanceledException
            or IOException
            or ObjectDisposedException
            or AggregateException)
        {
            // Best effort only; the original sanitized verification failure is retained.
        }
    }

    private static bool TryDecodeAscii(ReadOnlySpan<byte> value, out string result)
    {
        if (value.ContainsAnyExceptInRange((byte)0x20, (byte)0x7E))
        {
            result = string.Empty;
            return false;
        }

        result = Encoding.ASCII.GetString(value);
        return true;
    }

    private static bool IsObjectId(string value) =>
        value.Length is 40 or 64 && value.All(character => char.IsAsciiHexDigit(character));

    private sealed record GitInvocation(
        string RootPath,
        string AttributesFile,
        string GlobalConfigFile,
        string? GitDirectory,
        string? IndexFile,
        string? ObjectDirectory)
    {
        public static GitInvocation CreateDiscovery(string rootPath)
        {
            var emptyConfiguration = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
            return new GitInvocation(
                rootPath,
                emptyConfiguration,
                emptyConfiguration,
                GitDirectory: null,
                IndexFile: null,
                ObjectDirectory: null);
        }
    }

    private sealed record SourceRepository(
        string IndexPath,
        string? SharedIndexPath,
        string ObjectDirectory,
        string ObjectFormat);

    private sealed record IndexEntry(string Path, byte[] RawPath, string ObjectId);

    private enum BlobContentResult
    {
        Valid,
        ContainsNul,
        InvalidUtf8,
        Malformed
    }
}
