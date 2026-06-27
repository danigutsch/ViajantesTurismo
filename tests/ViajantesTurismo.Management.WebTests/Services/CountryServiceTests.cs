namespace ViajantesTurismo.Management.WebTests.Services;

public sealed class CountryServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetCountries_valid_json_returns_countries_ordered_by_name()
    {
        // Arrange
        CountryServiceTestsHelpers.WriteCountriesJson(_tempDir, """{"de": {"name": "Germany"}, "br": {"name": "Brazil"}}""");
        var sut = CountryServiceTestsHelpers.CreateService(_tempDir);

        // Act
        CountryInfo[] result = await sut.GetCountries(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Brazil", result[0].Name);
        Assert.Equal("BR", result[0].Code);
        Assert.Equal("Germany", result[1].Name);
        Assert.Equal("DE", result[1].Code);
    }

    [Fact]
    public async Task GetCountries_caches_result_on_second_call()
    {
        // Arrange
        CountryServiceTestsHelpers.WriteCountriesJson(_tempDir, """{"us": {"name": "United States"}}""");
        var sut = CountryServiceTestsHelpers.CreateService(_tempDir);

        // Act
        CountryInfo[] first = await sut.GetCountries(CancellationToken.None);
        CountryInfo[] second = await sut.GetCountries(CancellationToken.None);

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetCountries_missing_file_returns_fallback_countries()
    {
        // Arrange — data directory exists but file does not
        Directory.CreateDirectory(Path.Combine(_tempDir, "data"));
        var sut = CountryServiceTestsHelpers.CreateService(_tempDir);

        // Act
        CountryInfo[] result = await sut.GetCountries(CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, c => c.Code == "BR" && c.Name == "Brazil");
    }

    [Fact]
    public async Task GetCountries_invalid_json_returns_fallback_countries()
    {
        // Arrange
        CountryServiceTestsHelpers.WriteCountriesJson(_tempDir, "not valid json {{{");
        var sut = CountryServiceTestsHelpers.CreateService(_tempDir);

        // Act
        CountryInfo[] result = await sut.GetCountries(CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, c => c.Code == "US" && c.Name == "United States");
    }

    [Fact]
    public void NormalizeNationality_null_returns_empty_string()
    {
        // Act
        var result = CountryService.NormalizeNationality(null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void NormalizeNationality_empty_string_returns_empty_string()
    {
        // Act
        var result = CountryService.NormalizeNationality(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("Brazilian", "Brazil")]
    [InlineData("American", "United States")]
    [InlineData("British", "United Kingdom")]
    [InlineData("German", "Germany")]
    [InlineData("French", "France")]
    [InlineData("Italian", "Italy")]
    [InlineData("Spanish", "Spain")]
    [InlineData("Portuguese", "Portugal")]
    [InlineData("Canadian", "Canada")]
    [InlineData("Mexican", "Mexico")]
    public void NormalizeNationality_known_demonym_returns_country_name(string demonym, string expectedCountry)
    {
        // Act
        var result = CountryService.NormalizeNationality(demonym);

        // Assert
        Assert.Equal(expectedCountry, result);
    }

    [Fact]
    public void NormalizeNationality_known_demonym_is_case_insensitive()
    {
        // Act
        var result = CountryService.NormalizeNationality("brazilian");

        // Assert
        Assert.Equal("Brazil", result);
    }

    [Fact]
    public void NormalizeNationality_unknown_value_returns_original_value()
    {
        // Arrange
        const string unknown = "Martian";

        // Act
        var result = CountryService.NormalizeNationality(unknown);

        // Assert
        Assert.Equal(unknown, result);
    }

}
