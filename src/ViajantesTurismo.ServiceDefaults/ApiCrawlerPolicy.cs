namespace ViajantesTurismo.ServiceDefaults;

/// <summary>Search crawler policy for the application API hosts.</summary>
public static class ApiCrawlerPolicy
{
    /// <summary>The <c>robots.txt</c> body served by application APIs.</summary>
    public const string RobotsText = "User-agent: *\nDisallow: /";
}
