using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.ApiService;
using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.ApiService.Customers;
using ViajantesTurismo.Admin.Contracts;

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
        await using var app = builder.Build();
        app.UseRateLimiter();
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
        using var response = await client.PostAsync(new Uri("/customers/import", UriKind.Relative), requestContent, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }
}
