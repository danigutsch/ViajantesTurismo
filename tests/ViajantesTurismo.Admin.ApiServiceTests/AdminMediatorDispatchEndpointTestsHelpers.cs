using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.ApiService;
using ViajantesTurismo.Admin.Application;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Builders;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.ApiServiceTests;

/// <summary>Helpers for the mediator dispatch endpoint tests.</summary>
internal static class AdminMediatorDispatchEndpointTestsHelpers
{
    /// <summary>Creates an in-memory admin API host whose tour store and unit of work are the given fakes.</summary>
    public static WebApplicationFactory<AdminApiHostEntryPoint> CreateHost(FakeTourStore tourStore, FakeUnitOfWork unitOfWork) =>
        AdminApiTestHost.Create(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<ITourStore>(tourStore));
            services.Replace(ServiceDescriptor.Singleton<IUnitOfWork>(unitOfWork));
        });

    /// <summary>Creates the booking mutation request for the method and action, with a valid body where the endpoint needs one.</summary>
    public static HttpRequestMessage CreateBookingRequest(string method, Guid bookingId, string action)
    {
        var path = action.Length == 0 ? $"/api/v1/bookings/{bookingId}" : $"/api/v1/bookings/{bookingId}/{action}";
        return new HttpRequestMessage(new HttpMethod(method), new Uri(path, UriKind.Relative))
        {
            Content = action switch
            {
                "discount" => JsonContent.Create(DtoBuilders.BuildUpdateBookingDiscountDto()),
                "details" => JsonContent.Create(DtoBuilders.BuildUpdateBookingDetailsDto()),
                "notes" => JsonContent.Create(DtoBuilders.BuildUpdateBookingNotesDto()),
                _ => null
            }
        };
    }
}
