using SharedKernel.AspNetCore;
using ViajantesTurismo.Branding.ApiService;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddConfiguredTrustedForwardedHeaders(builder.Configuration.GetSection("Security:ForwardedHeaders"));
builder.Services.AddBrandingOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddBrandingSecurityBaseline(builder.Configuration);
builder.AddBrandingInfrastructure();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(BrandingSecurityBaseline.CorsPolicyName);
app.UseRateLimiter();
app.UseOutputCache();

app.MapBrandingEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
