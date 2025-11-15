using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Bookings.Commands.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.Commands.CreateBooking;
using ViajantesTurismo.Admin.Application.Bookings.Commands.DeleteBooking;
using ViajantesTurismo.Admin.Application.Customers.Commands.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.Commands.UpdateCustomer;
using ViajantesTurismo.Admin.Application.Tours.Commands.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.Commands.DeleteTour;
using ViajantesTurismo.Admin.Application.Tours.Commands.UpdateTour;

namespace ViajantesTurismo.Admin.Application;

/// <summary>
/// Provides extension methods for setting up the Application layer services in the application.
/// </summary>
public static class ApplicationDependencyInjection
{
    /// <summary>
    /// Adds the Application layer services to the application builder.
    /// </summary>
    /// <param name="builder">The application builder to configure.</param>
    /// <typeparam name="TApplicationBuilder">The type of the application builder, constrained to <see cref="IHostApplicationBuilder"/>.</typeparam>
    /// <returns>The updated application builder.</returns>
    public static TApplicationBuilder AddApplication<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<ConfirmBookingCommandHandler>();
        builder.Services.AddScoped<CreateBookingCommandHandler>();
        builder.Services.AddScoped<DeleteBookingCommandHandler>();
        builder.Services.AddScoped<CreateCustomerCommandHandler>();
        builder.Services.AddScoped<UpdateCustomerCommandHandler>();
        builder.Services.AddScoped<CreateTourCommandHandler>();
        builder.Services.AddScoped<DeleteTourCommandHandler>();
        builder.Services.AddScoped<UpdateTourCommandHandler>();

        return builder;
    }
}
