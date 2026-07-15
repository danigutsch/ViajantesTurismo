using ViajantesTurismo.AppHost;
using ViajantesTurismo.Resources;

var builder = DistributedApplication.CreateBuilder(args);
var profile = HostedProfileArguments.FromArguments(args);

builder.AddProductResources(profile);

await builder.Build().RunAsync();
