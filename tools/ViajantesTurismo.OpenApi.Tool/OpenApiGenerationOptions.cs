namespace ViajantesTurismo.OpenApi.Tool;

internal sealed record OpenApiGenerationOptions(OpenApiTarget Target, bool Refresh, string RepositoryRoot)
{
    public static OpenApiGenerationOptions Parse(string[] args, string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var target = args switch
        {
            ["generate", "admin"] or ["generate", "admin", "--refresh"] => OpenApiTarget.Admin,
            ["generate", "catalog"] or ["generate", "catalog", "--refresh"] => OpenApiTarget.Catalog,
            ["generate", "branding"] or ["generate", "branding", "--refresh"] => OpenApiTarget.Branding,
            _ => throw new ArgumentException("Expected: generate <admin|catalog|branding> [--refresh].")
        };

        return new OpenApiGenerationOptions(target, args.Length == 3, repositoryRoot);
    }
}
