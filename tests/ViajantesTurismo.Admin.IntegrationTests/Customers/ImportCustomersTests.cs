using System.Net;
using System.Net.Http.Json;
using System.Text;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

namespace ViajantesTurismo.Admin.IntegrationTests.Customers;

public sealed class ImportCustomersTests(ApiFixture fixture) : AdminApiIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Can_Import_Customers_From_Csv()
    {
        // Arrange
        var uniqueEmail = $"import.{Guid.NewGuid():N}@example.com";
        var csv =
            "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality,Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country,WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile\n" +
            $"John,Doe,Male,1990-01-01,USA,Engineer,A12345678,USA,{uniqueEmail},+1234567890,123 Main St,Downtown,10001,New York,NY,USA,75,175,Regular,DoubleOccupancy,SingleBed,Jane Doe,+0987654321";

        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        using var content = new MultipartFormDataContent();
        content.Add(fileContent, "file", "customers.csv");

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
}
