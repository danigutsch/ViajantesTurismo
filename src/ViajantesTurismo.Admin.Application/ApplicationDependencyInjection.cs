using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.RecordPayment;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.Application.Tours.UpdateTour;

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

        builder.Services.AddScoped<CancelBookingCommandHandler>();
        builder.Services.AddScoped<CompleteBookingCommandHandler>();
        builder.Services.AddScoped<ConfirmBookingCommandHandler>();
        builder.Services.AddScoped<CreateBookingCommandHandler>();
        builder.Services.AddScoped<DeleteBookingCommandHandler>();
        builder.Services.AddScoped<RecordPaymentCommandHandler>();
        builder.Services.AddScoped<UpdateBookingDetailsCommandHandler>();
        builder.Services.AddScoped<UpdateBookingDiscountCommandHandler>();
        builder.Services.AddScoped<UpdateBookingNotesCommandHandler>();
        builder.Services.AddScoped<CreateCustomerCommandHandler>();
        builder.Services.AddScoped<UpdateCustomerCommandHandler>();
        builder.Services.AddScoped<CreateTourCommandHandler>();
        builder.Services.AddScoped<DeleteTourCommandHandler>();
        builder.Services.AddScoped<UpdateTourCommandHandler>();

        return builder;
    }
}
