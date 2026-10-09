using SharedKernel.AspNetCore;
using SharedKernel.OpenApi;
using ViajantesTurismo.Branding.ApiService;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddBrandingOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddBrandingSecurityBaseline(builder.Configuration);
builder.Services.AddApiSecurity(
        builder.Configuration,
        builder.Environment,
        ApiAudienceNames.Branding,
        BrandingAuthorization.PermissionsByRole)
    .AddPolicy(BrandingAuthorization.BrandingRead, policy => policy.RequirePermission(BrandingAuthorization.BrandingRead))
    .AddPolicy(BrandingAuthorization.BrandingWrite, policy => policy.RequirePermission(BrandingAuthorization.BrandingWrite));
builder.AddBrandingInfrastructure(addOutboxRelay: !OpenApiGenerationMode.IsEnabled(builder.Environment));

var app = builder.Build();

app.UseForwardedHeaders();

app.MapConfiguredOpenApi();

app.UseCors(BrandingSecurityBaseline.CorsPolicyName);
app.UseApiSecurity();
app.UseRateLimiter();
app.UseOutputCache();

app.MapBrandingEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
