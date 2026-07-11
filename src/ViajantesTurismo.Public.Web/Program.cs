using SharedKernel.Branding;
using SharedKernel.HttpClients;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Public.Web;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClientDefaults();
builder.Services.AddHttpClient<IPublicCatalogApiClient, PublicCatalogApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.CatalogApi}"));
builder.Services.AddHttpClient<IBrandingApiClient, BrandingApiClient>(client => client.BaseAddress = new Uri($"https+http://{ResourceNames.BrandingApi}"));
builder.Services.AddRazorComponents();
builder.Services.AddOptions<PublicWebSitemapOptions>()
    .BindConfiguration(PublicWebSitemapOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<PublicWebSitemapOptions>, PublicWebSitemapOptionsValidator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UsePublicWebCacheHeaders();
app.UsePublicWebSecurityHeaders();
app.MapPublicWebEndpoints();

app.MapDefaultEndpoints();

await app.RunAsync();
