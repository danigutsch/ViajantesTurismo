using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.ServiceDefaults;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();
builder.Services.AddSingleton<ICatalogTourReadModelStore, InMemoryCatalogTourReadModelStore>();

var app = builder.Build();

app.MapGet("/catalog/tours", async (ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    var tours = await store.ListTours(ct);
    return tours.Select(MapTour);
});

app.MapGet("/public/catalog/tours", async (ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    var tours = await store.ListTours(ct);
    return tours.Where(IsPublished).Select(MapTour);
});

app.MapGet("/public/catalog/tours/{slug}", async (string slug, ICatalogTourReadModelStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(slug))
    {
        return Results.BadRequest();
    }

    var tour = await store.GetPublishedTourBySlug(slug, ct);
    return tour is null ? Results.NotFound() : Results.Ok(MapTour(tour));
});

app.MapDefaultEndpoints();

await app.RunAsync();

static CatalogTourDto MapTour(CatalogTourDraftReadModel tour)
{
    return new CatalogTourDto
    {
        Id = tour.CatalogTourId,
        AdminTourId = tour.AdminTourId,
        Identifier = tour.Identifier,
        Title = tour.Title,
        Slug = CreateSlug(tour.Identifier),
        IsPublished = IsPublished(tour),
        Images = [],
        UpdatedAt = tour.UpdatedAt
    };
}

static bool IsPublished(CatalogTourDraftReadModel tour)
{
    // Publish state is intentionally false until Catalog publish events are added to the read model.
    return false;
}

static string CreateSlug(string identifier) => identifier.Trim();
