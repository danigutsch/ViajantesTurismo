using PandaBear.Testing.AspNetCore;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PandaBear.MalwareScanning;
using PandaBear.Results;
using PandaBear.Testing;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Api.Customers.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.Testing.Logging;
using PandaBear.Testing.MalwareScanning;

namespace ViajantesTurismo.Admin.UnitTests.Api.Customers.Import;

[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class CustomerImportEndpointsTests
{
    [Fact]
    public void Accepts_small_CSV_uploads()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeTrue();
        problem.ShouldBeNull();
    }

    [Fact]
    public void Accepts_CSV_uploads_with_media_type_parameters()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv; charset=utf-8"
        };

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeTrue();
        problem.ShouldBeNull();
    }

    [Fact]
    public void Rejects_empty_uploads_with_generic_problem_details()
    {
        // Arrange
        using var stream = new MemoryStream();
        var file = new FormFile(stream, 0, 0, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Invalid customer import file.");
        problem.Detail.ShouldBe("Upload a CSV file that meets the documented import requirements.");
    }

    [Fact]
    public void Rejects_missing_uploads_with_generic_problem_details()
    {
        // Arrange
        IFormFile? file = null;

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Invalid customer import file.");
        problem.Detail.ShouldBe("Upload a CSV file that meets the documented import requirements.");
    }

    [Fact]
    public void Rejects_non_CSV_uploads()
    {
        // Arrange
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
    }

    [Fact]
    public void Rejects_oversized_uploads()
    {
        // Arrange
        using var stream = new MemoryStream([1]);
        var file = new FormFile(stream, 0, ContractConstants.CustomerImportMaxFileBytes + 1, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };

        // Act
        var isValid = CustomerImportEndpoints.TryValidateImportFile(file, out var problem);

        // Assert
        isValid.ShouldBeFalse();
        problem.ShouldNotBeNull();
    }

    [Fact]
    public void Request_limit_includes_file_conflict_resolution_and_multipart_budgets()
    {
        // Arrange
        const long expectedBudget =
            ContractConstants.CustomerImportMaxFileBytes
            + ContractConstants.CustomerImportConflictResolutionsMaxBytes
            + ContractConstants.CustomerImportMultipartEnvelopeBytes;

        // Act
        var requestLimit = ContractConstants.CustomerImportMaxRequestBytes;

        // Assert
        requestLimit.ShouldBe(expectedBudget);
    }

    [Fact]
    public async Task Reads_CSV_only_after_a_clean_scan_of_the_received_bytes()
    {
        // Arrange
        var expectedCsv = "firstName,lastName\nAda,Lovelace";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(expectedCsv));
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Encoding.UTF8.GetString(result.Value.Span).ShouldBe(expectedCsv);
        scanner.LastScan.ShouldNotBeNull().Content.ToArray().ShouldBe(Encoding.UTF8.GetBytes(expectedCsv));
    }

    [Fact]
    public async Task Rejects_CSV_content_when_the_scanner_reports_malware()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new TestMalwareScanner { Result = MalwareScanResult.Rejected, ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import file did not pass malware scanning.");
    }

    [Fact]
    public async Task Fails_closed_when_the_CSV_scanner_is_unavailable()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new TestMalwareScanner { Result = new MalwareScanResult(MalwareScanStatus.Failed), ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Unavailable);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import scanner is unavailable.");
    }

    [Fact]
    public async Task Fails_closed_when_the_CSV_scanner_throws()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = TestMalwareScanner.Throwing(new IOException());

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Unavailable);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import scanner is unavailable.");
    }

    [Fact]
    public async Task Logs_a_structured_warning_without_exception_text_or_file_name_when_the_CSV_scanner_throws()
    {
        // Arrange
        const string sensitiveText = "clamd refused customers-private.csv";
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers-private.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = TestMalwareScanner.Throwing(new IOException(sensitiveText));
        var logger = new RecordingLogger();

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, logger, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.Unavailable);
        var entry = logger.Records.ShouldHaveSingleItem();
        entry.Level.ShouldBe(LogLevel.Warning);
        entry.EventId.Id.ShouldBe(1);
        entry.EventId.Name.ShouldBe("CustomerImportScannerFailed");
        entry.State.ShouldContain(value => value.Key == "FailureType" && Equals(value.Value, nameof(IOException)));
        entry.Message.ShouldNotContain(sensitiveText, StringComparison.Ordinal);
        entry.Message.ShouldNotContain("customers-private", StringComparison.Ordinal);
        entry.Exception.ShouldBeNull();
    }

    [Fact]
    public async Task ReadCsv_propagates_cancellation_during_bounded_buffering_without_scanning()
    {
        // Arrange
        var content = "firstName,lastName\nAda,Lovelace"u8.ToArray();
        using var stream = new GatedCountingReadStream(content);
        var file = new FormFile(stream, 0, content.LongLength, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var read = CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, requestCancellation.Token);
        await stream.ReadStarted.WaitAsync(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await read;

        // Assert
        _ = await action.ShouldThrow<TaskCanceledException>();
        scanner.CallCount.ShouldBe(0);
        stream.BytesRead.ShouldBe(0);
    }

    [Fact]
    public async Task ReadCsv_scans_the_received_file_and_returns_its_bytes_from_a_seekable_stream()
    {
        // Arrange
        var content = "firstName,lastName\nAda,Lovelace"u8.ToArray();
        using var stream = new GatedCountingReadStream(content);
        stream.Release();
        var file = new FormFile(stream, 0, content.LongLength, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        scanner.LastScan.ShouldNotBeNull().Content.ToArray().ShouldBe(content);
        result.Value.ToArray().ShouldBe(content);
    }

    [Fact]
    public async Task ReadCsv_allocates_one_array_of_the_content_for_a_large_seekable_file()
    {
        // Arrange
        var content = new byte[ContractConstants.CustomerImportMaxFileBytes];
        Array.Fill(content, (byte)'a');
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Drain };
        var warmUp = new FormFile(new MemoryStream(content), 0, content.LongLength, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var measured = new FormFile(new MemoryStream(content), 0, content.LongLength, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        _ = await CustomerImportEndpoints.ReadCsv(warmUp, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = await CustomerImportEndpoints.ReadCsv(measured, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Length.ShouldBe(content.Length);
        allocated.ShouldBeLessThan(content.Length * 3 / 2);
    }

    [Theory]
    [InlineData("utf8")]
    [InlineData("utf8-bom")]
    [InlineData("utf16-bom")]
    public async Task ReadCsv_decodes_the_received_text_and_drops_the_byte_order_mark(string encoding)
    {
        // Arrange
        const string text = "firstName,lastName\nJosé,Müller";
        var content = encoding switch
        {
            "utf8" => Encoding.UTF8.GetBytes(text),
            "utf8-bom" => [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text)],
            _ => [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)]
        };
        using var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.LongLength, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture }, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Encoding.UTF8.GetString(result.Value.Span).ShouldBe(text);
    }

    [Fact]
    public async Task ReadCsv_buffers_a_slow_non_seekable_source_chunk_by_chunk_and_scans_only_after_the_last_chunk()
    {
        // Arrange
        var content = "firstName,lastName\nAda,Lovelace\nGrace,Hopper"u8.ToArray();
        using var stream = new GatedChunkReadStream(content, 7);
        var file = new StreamFormFile(stream, content.LongLength, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };
        var chunkReads = (content.Length + 6) / 7;

        // Act
        var read = CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);
        for (var i = 0; i < chunkReads; i++)
        {
            await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
            scanner.CallCount.ShouldBe(0);
            stream.ReleaseRead();
        }

        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        stream.ReleaseRead();
        var result = await read;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Encoding.UTF8.GetString(result.Value.Span).ShouldBe(Encoding.UTF8.GetString(content));
        scanner.LastScan.ShouldNotBeNull().Content.ToArray().ShouldBe(content);
        stream.LargestRequestedRead.ShouldBeLessThanOrEqualTo(128 * 1024);
    }

    [Fact]
    public async Task ReadCsv_accepts_a_source_exactly_at_the_size_limit()
    {
        // Arrange
        var content = new byte[ContractConstants.CustomerImportMaxFileBytes];
        Array.Fill(content, (byte)'a');
        using var stream = new GatedChunkReadStream(content, 64 * 1024);
        stream.ReleaseAll();
        var file = new StreamFormFile(stream, content.LongLength, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Length.ShouldBe(content.Length);
        scanner.LastScan.ShouldNotBeNull().Content.Length.ShouldBe(content.Length);
    }

    [Fact]
    public async Task ReadCsv_rejects_a_source_one_byte_over_the_limit_without_scanning_even_when_the_declared_length_is_within_it()
    {
        // Arrange
        var content = new byte[ContractConstants.CustomerImportMaxFileBytes + 1];
        Array.Fill(content, (byte)'a');
        using var stream = new GatedChunkReadStream(content, 64 * 1024);
        stream.ReleaseAll();
        var file = new StreamFormFile(stream, ContractConstants.CustomerImportMaxFileBytes, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        scanner.CallCount.ShouldBe(0);
        stream.BytesRead.ShouldBeLessThanOrEqualTo(ContractConstants.CustomerImportMaxFileBytes + 64 * 1024);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task ReadCsv_rejects_a_declared_length_that_differs_from_the_received_bytes_without_scanning(int declaredDelta)
    {
        // Arrange
        var content = "firstName,lastName\nAda,Lovelace"u8.ToArray();
        using var stream = new GatedChunkReadStream(content, 8);
        stream.ReleaseAll();
        var file = new StreamFormFile(stream, content.LongLength + declaredDelta, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        scanner.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReadCsv_cancels_a_slow_source_after_partial_data_without_scanning_and_disposes_it()
    {
        // Arrange
        var content = "firstName,lastName\nAda,Lovelace"u8.ToArray();
        using var stream = new GatedChunkReadStream(content, 8);
        var file = new StreamFormFile(stream, content.LongLength, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture };
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var read = CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, requestCancellation.Token);
        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        stream.ReleaseRead();
        await stream.WaitForPendingRead(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await read;

        // Assert
        _ = await action.ShouldThrowAssignableTo<OperationCanceledException>();
        scanner.CallCount.ShouldBe(0);
        stream.BytesRead.ShouldBe(8);
        stream.IsDisposed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("passed")]
    [InlineData("rejected")]
    [InlineData("unavailable")]
    [InlineData("throws")]
    [InlineData("over-limit")]
    public async Task ReadCsv_disposes_the_source_stream_on_every_outcome(string outcome)
    {
        // Arrange
        var content = outcome is "over-limit"
            ? new byte[ContractConstants.CustomerImportMaxFileBytes + 1]
            : "firstName,lastName\nAda,Lovelace"u8.ToArray();
        using var stream = new GatedChunkReadStream(content, 64 * 1024);
        stream.ReleaseAll();
        var file = new StreamFormFile(stream, content.LongLength, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = outcome switch
        {
            "rejected" => new TestMalwareScanner { Result = MalwareScanResult.Rejected, ContentHandling = MalwareScanContentHandling.Capture },
            "unavailable" => new TestMalwareScanner { Result = new MalwareScanResult(MalwareScanStatus.Failed), ContentHandling = MalwareScanContentHandling.Capture },
            "throws" => TestMalwareScanner.Throwing(new IOException()),
            _ => new TestMalwareScanner { ContentHandling = MalwareScanContentHandling.Capture }
        };

        // Act
        _ = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        stream.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadCsv_scans_the_raw_received_bytes_before_transcoding_and_returns_nothing_when_the_scanner_rejects()
    {
        // Arrange
        var content = new byte[] { 0xFF, 0xFE }.Concat(Encoding.Unicode.GetBytes("firstName,lastName\nAda,Lovelace")).ToArray();
        using var stream = new GatedChunkReadStream(content, 5);
        stream.ReleaseAll();
        var file = new StreamFormFile(stream, content.LongLength, "customers.csv", ContractConstants.CustomerImportTextCsvContentType);
        var scanner = new TestMalwareScanner { Result = MalwareScanResult.Rejected, ContentHandling = MalwareScanContentHandling.Capture };

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, NullLogger.Instance, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        scanner.LastScan.ShouldNotBeNull().Content.ToArray().ShouldBe(content);
    }
}
