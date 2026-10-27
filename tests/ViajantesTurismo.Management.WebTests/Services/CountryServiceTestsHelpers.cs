namespace ViajantesTurismo.Management.WebTests.Services;

/// <summary>Helpers for building <see cref="CountryService"/> instances over a temporary web root.</summary>
internal static class CountryServiceTestsHelpers
{
    /// <summary>Creates a <see cref="CountryService"/> backed by a stub environment rooted at <paramref name="webRootPath"/>.</summary>
    public static CountryService CreateService(string webRootPath)
    {
        var env = new StubWebHostEnvironment(webRootPath);
        return new CountryService(env);
    }

    /// <summary>Writes <paramref name="json"/> to <c>data/countries.json</c> under <paramref name="webRootPath"/>, creating the folder.</summary>
    public static void WriteCountriesJson(string webRootPath, string json)
    {
        var dataDir = Path.Combine(webRootPath, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "countries.json"), json);
    }
}
