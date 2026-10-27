namespace ViajantesTurismo.Architecture.Analyzers.Tests;

/// <summary>Support source compiled alongside analyzer test inputs.</summary>
internal static class AnalyzerTestSupport
{
    /// <summary>C# source of the support types referenced by analyzer test inputs.</summary>
    public const string Source = """
        using System;
        using System.Net.Http;

        namespace Demo
        {
            public sealed class ServiceCollection
            {
            }
        }

        namespace System.Net.Http
        {
            public interface IHttpClientFactory
            {
                HttpClient CreateClient(string name);
            }
        }

        namespace Microsoft.Extensions.DependencyInjection
        {
            public static class HttpClientFactoryServiceCollectionExtensions
            {
                public static object AddHttpClient(this Demo.ServiceCollection services, string name)
                {
                    throw new NotSupportedException();
                }

                public static object AddHttpClient(this Demo.ServiceCollection services, string name, Action<HttpClient> configureClient)
                {
                    throw new NotSupportedException();
                }

                public static object AddHttpClient<TClient, TImplementation>(this Demo.ServiceCollection services)
                    where TImplementation : TClient
                {
                    throw new NotSupportedException();
                }

                public static object AddHttpClient<TClient, TImplementation>(this Demo.ServiceCollection services, Action<HttpClient> configureClient)
                    where TImplementation : TClient
                {
                    throw new NotSupportedException();
                }
            }
        }

        namespace PandaBear.Branding
        {
            public interface IBrandingApiClient
            {
            }

            public sealed class BrandingApiClient : IBrandingApiClient
            {
            }
        }

        namespace ViajantesTurismo.Resources
        {
            public static class ResourceNames
            {
                public const string Api = "api";
                public const string CatalogApi = "catalog-api";
                public const string IdentityProvider = "identity-provider";
                public const string SeaweedFs = "seaweedfs";
                public const string OpenTelemetryCollector = "opentelemetry-collector";
            }
        }
        """;
}
