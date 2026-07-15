using Microsoft.Extensions.Hosting;
using ViajantesTurismo.DatabaseObservability;

var builder = Host.CreateApplicationBuilder(args);
DatabaseObservabilityHostConfiguration.Configure(builder);

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
