using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();
