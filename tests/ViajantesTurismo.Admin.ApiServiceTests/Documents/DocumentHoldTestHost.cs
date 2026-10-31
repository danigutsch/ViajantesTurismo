using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.DispositionHold;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Application.Documents.Holds;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests.Documents;

/// <summary>Composes the Admin API test host with in-memory hold services and one known synthetic document.</summary>
internal static class DocumentHoldTestHost
{
    /// <summary>Creates the host so <paramref name="documentId" /> resolves to a lineage and holds live in <paramref name="holds" />.</summary>
    public static WebApplicationFactory<AdminApiHostEntryPoint> Create(Guid documentId, InMemoryDispositionHoldStore holds) =>
        AdminApiTestHost.Create(services =>
        {
            var query = new FakeDocumentHoldQuery(holds);
            query.LineageByDocument[documentId] = Guid.CreateVersion7();
            services.RemoveAll<IDocumentHoldQuery>();
            services.AddSingleton<IDocumentHoldQuery>(query);
            services.RemoveAll<IDispositionHoldStore>();
            services.AddSingleton<IDispositionHoldStore>(holds);
            services.RemoveAll<IDocumentDispositionTransaction>();
            services.AddSingleton<IDocumentDispositionTransaction>(new FakeDocumentDispositionTransaction());
            services.RemoveAll<IUnitOfWork>();
            services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        });
}
