namespace ViajantesTurismo.Management.WebTests.Services;

internal static class CountryServiceTestsHelpers
{
    public static CountryService CreateService(string webRootPath)
    {
        var env = new StubWebHostEnvironment(webRootPath);
        return new CountryService(env);
    }

    public static void WriteCountriesJson(string webRootPath, string json)
    {
        var dataDir = Path.Combine(webRootPath, "data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "countries.json"), json);
    }
}
