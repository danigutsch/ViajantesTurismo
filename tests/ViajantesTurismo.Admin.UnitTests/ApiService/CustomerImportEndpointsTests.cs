using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.MalwareScanning;
using SharedKernel.Results;
using ViajantesTurismo.Admin.ApiService;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.ApiService.Customers;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
public sealed class CustomerImportEndpointsTests
{
    [Fact]
    public void Accepts_small_csv_uploads()
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
    public void Accepts_csv_uploads_with_media_type_parameters()
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
    public void Rejects_non_csv_uploads()
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
    public async Task Reads_csv_only_after_a_clean_scan_of_the_received_bytes()
    {
        // Arrange
        var expectedCsv = "firstName,lastName\nAda,Lovelace";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(expectedCsv));
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new StubMalwareScanner(MalwareScanResult.Passed);

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedCsv);
        scanner.ScannedContent.ShouldBe(System.Text.Encoding.UTF8.GetBytes(expectedCsv));
    }

    [Fact]
    public async Task Rejects_csv_content_when_the_scanner_reports_malware()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new StubMalwareScanner(new MalwareScanResult(MalwareScanStatus.Rejected));

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import file did not pass malware scanning.");
    }

    [Fact]
    public async Task Fails_closed_when_the_csv_scanner_is_unavailable()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new StubMalwareScanner(new MalwareScanResult(MalwareScanStatus.Failed));

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Unavailable);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import scanner is unavailable.");
    }

    [Fact]
    public async Task Fails_closed_when_the_csv_scanner_throws()
    {
        // Arrange
        using var stream = new MemoryStream("firstName,lastName\nAda,Lovelace"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "customers.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = ContractConstants.CustomerImportTextCsvContentType
        };
        var scanner = new StubMalwareScanner(MalwareScanResult.Passed, new IOException());

        // Act
        var result = await CustomerImportEndpoints.ReadCsv(file, scanner, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Unavailable);
        result.ErrorDetails.ShouldNotBeNull();
        result.ErrorDetails.Detail.ShouldBe("Customer import scanner is unavailable.");
    }

    [Fact]
    public async Task Rejects_requests_larger_than_the_import_request_limit()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddAdminSecurityBaseline(builder.Configuration);
        builder.Services.AddAuthorization(options =>
            options.AddPolicy(AdminAuthorization.CustomerImport, policy => policy.RequireAssertion(static _ => true)));
        await using var app = builder.Build();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapCustomerImportEndpoints();

        await app.StartAsync(cancellationToken);
        using var client = new HttpClient
        {
            BaseAddress = new Uri(app.Urls.Single())
        };
        using var requestContent = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(new byte[ContractConstants.CustomerImportMaxRequestBytes + 1]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContractConstants.CustomerImportTextCsvContentType);
        requestContent.Add(fileContent, "file", "customers.csv");

        // Act
        using var response = await client.PostAsync(new Uri("/api/v1/customers/import", UriKind.Relative), requestContent, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }
}
