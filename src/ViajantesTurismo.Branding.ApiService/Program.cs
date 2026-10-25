using PandaBear.AspNetCore;
using PandaBear.OpenApi;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddBrandingOpenApiDocuments();
builder.Services.AddOutputCache();
builder.Services.AddApiSecurity(
        builder.Configuration,
        builder.Environment,
        ApiAudienceNames.Branding,
        BrandingModule.PermissionsByRole)
    .AddBrandingPolicies(authenticationScheme: null);
builder.AddBrandingModule(addOutboxRelay: !OpenApiGenerationMode.IsEnabled(builder.Environment));

var app = builder.Build();

app.UseForwardedHeaders();

app.MapConfiguredOpenApi();

app.UseCors(BrandingSecurityBaseline.CorsPolicyName);
app.UseApiSecurity();
app.UseRateLimiter();
app.UseOutputCache();

app.MapBrandingModule();

app.MapDefaultEndpoints();

await app.RunAsync().ConfigureAwait(false);
