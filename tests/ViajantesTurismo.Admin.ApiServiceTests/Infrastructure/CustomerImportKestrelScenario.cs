using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.MalwareScanning;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Fakes;
using ViajantesTurismo.UploadWire.Testing;

namespace ViajantesTurismo.Admin.ApiServiceTests.Infrastructure;

/// <summary>
/// The Admin customer import endpoints on a real loopback Kestrel server, with a recording scanner, a fake customer
/// store and a probe that observes the request token and the body reads on the server side.
/// </summary>
internal sealed class CustomerImportKestrelScenario : IAsyncDisposable
{
    /// <summary>The import preview/apply route.</summary>
    public const string ImportRoute = "/api/v1/customers/import/";

    /// <summary>The import commit route.</summary>
    public const string CommitRoute = "/api/v1/customers/import/commit";

    /// <summary>A valid import CSV with one customer.</summary>
    public const string ValidCsv =
        "FirstName,LastName,Gender,BirthDate,Nationality,Occupation,NationalId,IdNationality," +
        "Email,Mobile,Street,Neighborhood,PostalCode,City,State,Country," +
        "WeightKg,HeightCentimeters,BikeType,RoomType,BedType,EmergencyContactName,EmergencyContactMobile\n" +
        "Ana,Silva,Male,1990-01-01,Brazilian,Engineer,A12345678,BR," +
        "ana@example.com,+5511999999999,Rua A,Centro,01000-000,Sao Paulo,SP,Brazil," +
        "75,175,Regular,DoubleOccupancy,SingleBed,Emergency Name,+5511888888888";

    private readonly WebApplicationFactory<AdminApiHostEntryPoint> root;
    private readonly WebApplicationFactory<AdminApiHostEntryPoint> factory;

    private CustomerImportKestrelScenario(
        WebApplicationFactory<AdminApiHostEntryPoint> root,
        WebApplicationFactory<AdminApiHostEntryPoint> factory,
        Uri baseUri,
        RequestLifetimeProbe probe,
        ScanRecorder scans,
        FakeCustomerStore store,
        FakeUnitOfWork unitOfWork)
    {
        this.root = root;
        this.factory = factory;
        BaseUri = baseUri;
        Probe = probe;
        Scans = scans;
        Store = store;
        UnitOfWork = unitOfWork;
        Client = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(60) };
        AdminApiTestHost.ConfigureAuthenticatedClient(Client, "Admin");
    }

    /// <summary>The loopback address of the server.</summary>
    public Uri BaseUri
    {
        get;
    }

    /// <summary>An authenticated client for the server.</summary>
    public HttpClient Client
    {
        get;
    }

    /// <summary>Server-side observation of the import request.</summary>
    public RequestLifetimeProbe Probe
    {
        get;
    }

    /// <summary>The scans the malware scanner received.</summary>
    public ScanRecorder Scans
    {
        get;
    }

    /// <summary>The customer store the import handler persists into.</summary>
    public FakeCustomerStore Store
    {
        get;
    }

    /// <summary>The unit of work the import handler commits through.</summary>
    public FakeUnitOfWork UnitOfWork
    {
        get;
    }

    /// <summary>Gets the Authorization header value of the client.</summary>
    public string Authorization => Client.DefaultRequestHeaders.Authorization!.ToString();

    /// <summary>Starts the server; <paramref name="routeSuffix"/> selects the observed route.</summary>
    public static CustomerImportKestrelScenario Start(string routeSuffix, Action<IWebHostBuilder>? configureHost = null)
    {
        var probe = new RequestLifetimeProbe(routeSuffix);
        var scans = new ScanRecorder();
        var store = new FakeCustomerStore();
        var unitOfWork = new FakeUnitOfWork();
        var root = AdminApiTestHost.Create(services =>
        {
            RequestProbeStartupFilter.Register(services, probe);
            services.Replace(ServiceDescriptor.Singleton<IMalwareScanner>(new RecordingMalwareScanner(scans)));
            var handler = new CustomerImportCommandHandler(store, unitOfWork, TimeProvider.System);
            services.Replace(ServiceDescriptor.Singleton(new CustomerImportWorkflowService(
                store,
                new CustomerImportConflictDetector(store),
                handler,
                TimeProvider.System)));
        });
        var factory = root.WithWebHostBuilder(host =>
        {
            host.UseRandomLoopbackPort();
            configureHost?.Invoke(host);
        });

        var baseUri = factory.Start();
        return new CustomerImportKestrelScenario(root, factory, baseUri, probe, scans, store, unitOfWork);
    }

    /// <summary>Builds the multipart form for a CSV file, with the empty resolutions field the commit route accepts.</summary>
    public static async Task<MultipartPayload> Form(byte[] csv, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        using var file = MultipartPayload.FilePart(csv, ContractConstants.CustomerImportTextCsvContentType);
        form.Add(file, "file", "customers.csv");
        return await MultipartPayload.From(form, ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
        await root.DisposeAsync();
    }
}
