using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal static class SeaweedFsMediaObjectStorageDependencyInjection
{
    public static IServiceCollection AddSeaweedFsMediaObjectStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<SeaweedFsMediaObjectStorageOptions>()
            .BindConfiguration(SeaweedFsMediaObjectStorageOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<SeaweedFsMediaObjectStorageOptions>, SeaweedFsMediaObjectStorageOptionsValidator>());
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<SeaweedFsMediaObjectStorageOptions>>().Value;
            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), new AmazonS3Config
            {
                ServiceURL = options.Endpoint!.AbsoluteUri,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1"
            });
        });
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics.AddAWSInstrumentation());
        services.AddSingleton<IMediaObjectStore, SeaweedFsMediaObjectStore>();

        return services;
    }
}
