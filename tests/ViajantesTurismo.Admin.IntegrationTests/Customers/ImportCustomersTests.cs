using System.Net;
using System.Net.Http.Json;
using System.Text;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;
using ViajantesTurismo.Admin.Tests.Shared.Integration.Helpers;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class ImportCustomersTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    private static string BuildCanonicalCsv(string email)
    {
        return
            "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality,Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country,WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile\n" +
            $"John,Doe,Male,1990-01-01,USA,Engineer,A12345678,USA,{email},+1234567890,123 Main St,Downtown,10001,New York,NY,USA,75,175,Regular,DoubleOccupancy,SingleBed,Jane Doe,+0987654321";
    }

    private static MultipartFormDataContent BuildCsvMultipartContent(string csv)
    {
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        var content = new MultipartFormDataContent
        {
            { fileContent, "file", "customers.csv" }
        };
        return content;
    }

    [Fact]
    public async Task Can_Import_Customers_From_Csv()
    {
        // Arrange
        var uniqueEmail = $"import.{Guid.NewGuid():N}@example.com";
        var csv = BuildCanonicalCsv(uniqueEmail);

        using var content = BuildCsvMultipartContent(csv);

        // Act
        var response = await Client.PostAsync(
            new Uri("/customers/import", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected OK but got {response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task Can_Return_Conflicts_When_Imported_Email_Already_Exists()
    {
        // Arrange
        var duplicateEmail = TestDataGenerator.UniqueEmail("import-duplicate");
        var existingCustomer = DtoBuilders.BuildCreateCustomerDto("Existing", "Customer") with
        {
            ContactInfo = DtoBuilders.BuildCreateCustomerDto("Existing", "Customer").ContactInfo with { Email = duplicateEmail }
        };

        var createResponse = await Client.PostAsJsonAsync(
            new Uri("/customers", UriKind.Relative),
            existingCustomer,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var csv = BuildCanonicalCsv(duplicateEmail);
        using var content = BuildCsvMultipartContent(csv);

        // Act
        var response = await Client.PostAsync(
            new Uri("/customers/import", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected OK but got {response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.NotNull(result.Conflicts);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal(duplicateEmail, conflict.Email, ignoreCase: true);
    }

    [Fact]
    public async Task Can_Commit_Import_With_Keep_Resolution_For_Existing_Email()
    {
        // Arrange
        var duplicateEmail = TestDataGenerator.UniqueEmail("import-commit-keep");
        var existingCustomer = DtoBuilders.BuildCreateCustomerDto("Existing", "Customer") with
        {
            ContactInfo = DtoBuilders.BuildCreateCustomerDto("Existing", "Customer").ContactInfo with { Email = duplicateEmail }
        };

        var createResponse = await Client.PostAsJsonAsync(
            new Uri("/customers", UriKind.Relative),
            existingCustomer,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var csv = BuildCanonicalCsv(duplicateEmail);
        using var content = BuildCsvMultipartContent(csv);
        content.Add(
            new StringContent(
                ConflictResolutionSerialization.Serialize(
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [duplicateEmail] = "keep"
                    })),
            "conflictResolutions");

        // Act
        var response = await Client.PostAsync(
            new Uri("/customers/import/commit", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Fail($"Expected OK but got {response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
    }
}
