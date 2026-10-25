using PandaBear.Branding;
using PandaBear.HttpClients;
using Microsoft.Extensions.Options;
using PandaBear.AspNetCore;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClientDefaults(PublicWebSecurityBaseline.ConfigureCatalogResilience);
builder.AddConfiguredTrustedForwardedHeaders();
builder.Services.AddPublicWebSecurityBaseline();
builder.Services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"))
    .AddForwardedClientIp();
builder.Services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddRazorComponents();
builder.Services.AddOptions<PublicWebSitemapOptions>()
    .BindConfiguration(PublicWebSitemapOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PublicWebSitemapOptions>, PublicWebSitemapOptionsValidator>();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UsePublicWebCacheHeaders();
app.UsePublicWebSecurityHeaders();
app.UseRateLimiter();
app.MapPublicWebEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
