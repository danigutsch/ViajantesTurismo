using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Admin.Application.Payments;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Bookings.CancelBooking;
using ViajantesTurismo.Admin.Application.Bookings.CompleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDiscount;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingNotes;
using ViajantesTurismo.Admin.Application.Customers.CreateCustomer;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Customers.UpdateCustomer;
using ViajantesTurismo.Admin.Application.Documents;
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

        builder.Services.AddScoped<PaymentInstructionWorkflow>();
        builder.Services.AddScoped<GeneratePaymentInstructionsCommandHandler>();
        builder.Services.AddScoped<ApprovePaymentInstructionsCommandHandler>();
        builder.Services.AddScoped<FinalizePaymentInstructionsCommandHandler>();
        builder.Services.AddScoped<WithdrawPaymentInstructionApprovalCommandHandler>();
        builder.Services.AddScoped<VoidPaymentInstructionsCommandHandler>();
        builder.Services.AddScoped<ReviewPaymentInstructionsCommandHandler>();
        builder.Services.AddScoped<BookingPaymentCommandExecutor>();
        builder.Services.AddScoped<DraftBookingPaymentScheduleCommandHandler>();
        builder.Services.AddScoped<ApproveBookingPaymentScheduleCommandHandler>();
        builder.Services.AddScoped<DraftBookingPaymentQuoteCommandHandler>();
        builder.Services.AddScoped<ApproveBookingPaymentQuoteCommandHandler>();
        builder.Services.AddScoped<RecordBookingReceiptCommandHandler>();
        builder.Services.AddScoped<DraftBookingReceiptReconciliationCommandHandler>();
        builder.Services.AddScoped<ApproveBookingReceiptReconciliationCommandHandler>();
        builder.Services.AddScoped<AllocateBookingPaymentCommandHandler>();
        builder.Services.AddScoped<ReleaseBookingPaymentAllocationsCommandHandler>();
        builder.Services.AddScoped<ReverseBookingReceiptCommandHandler>();
        builder.Services.AddScoped<DraftBookingPaymentRefundCommandHandler>();
        builder.Services.AddScoped<ApproveBookingPaymentRefundCommandHandler>();
        builder.Services.AddScoped<CorrectBookingPaymentRefundCommandHandler>();
        builder.Services.AddScoped<ReviewBookingFinancialRecordCommandHandler>();
        builder.Services.AddScoped<WithdrawBookingFinancialApprovalCommandHandler>();
        builder.Services.AddScoped<PaymentCommandExecutor>();
        builder.Services.AddScoped<PaymentAccountCommandExecutor>();
        builder.Services.AddScoped<CreatePaymentAccountCommandHandler>();
        builder.Services.AddScoped<UpdatePaymentAccountCommandHandler>();
        builder.Services.AddScoped<VerifyPaymentAccountCommandHandler>();
        builder.Services.AddScoped<ReviewPaymentAccountCommandHandler>();
        builder.Services.AddScoped<RetirePaymentAccountCommandHandler>();
        builder.Services.AddScoped<WithdrawPaymentAccountVerificationCommandHandler>();

        builder.Services.AddScoped<CancelBookingCommandHandler>();
        builder.Services.AddScoped<CompleteBookingCommandHandler>();
        builder.Services.AddScoped<ConfirmBookingCommandHandler>();
        builder.Services.AddScoped<CreateBookingCommandHandler>();
        builder.Services.AddScoped<DeleteBookingCommandHandler>();
        builder.Services.AddScoped<UpdateBookingDetailsCommandHandler>();
        builder.Services.AddScoped<UpdateBookingDiscountCommandHandler>();
        builder.Services.AddScoped<UpdateBookingNotesCommandHandler>();
        builder.Services.AddScoped<CreateCustomerCommandHandler>();
        builder.Services.AddScoped<CustomerImportCommandHandler>();
        builder.Services.AddScoped<CustomerImportConflictDetector>();
        builder.Services.AddScoped<CustomerImportWorkflowService>();
        builder.Services.AddScoped<UpdateCustomerCommandHandler>();
        builder.Services.AddScoped<GenerateContractDraftCommandHandler>();
        builder.Services.AddDocumentBranding(builder.Configuration);
        builder.Services.AddScoped<DocumentCommandIdempotency>();
        builder.Services.AddScoped<DocumentAuditWriter>();
        builder.Services.AddScoped<BeginDocumentReviewCommandHandler>();
        builder.Services.AddScoped<RequestDocumentChangesCommandHandler>();
        builder.Services.AddScoped<UpdateDocumentFieldCommandHandler>();
        builder.Services.AddScoped<ApproveDocumentCommandHandler>();
        builder.Services.AddScoped<FinalizeDocumentCommandHandler>();
        builder.Services.AddScoped<GetFinalizedDocumentArtifactHandler>();
        builder.Services.AddScoped<RegenerateDocumentDraftCommandHandler>();
        builder.Services.AddScoped<VoidDocumentCommandHandler>();
        builder.Services.AddScoped<PurgeExpiredDraftsCommandHandler>();
        builder.Services.AddScoped<PurgeExpiredDocumentAuditsCommandHandler>();
        builder.Services.AddScoped<CreateTourCommandHandler>();
        builder.Services.AddScoped<DeleteTourCommandHandler>();
        builder.Services.AddScoped<UpdateTourCommandHandler>();
        builder.Services.AddDomainEventProcessing();

        return builder;
    }

    /// <summary>
    /// Adds document Branding capture. The source mode is read from <see cref="DocumentBrandingOptions.SectionName" />
    /// and defaults to HTTP; an unrecognized value fails startup instead of silently choosing a source.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDocumentBranding(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredSource = configuration[$"{DocumentBrandingOptions.SectionName}:{nameof(DocumentBrandingOptions.Source)}"];
        var source = DocumentBrandingSourceMode.Http;
        if (!string.IsNullOrWhiteSpace(configuredSource)
            && (!Enum.TryParse(configuredSource, ignoreCase: true, out source) || !Enum.IsDefined(source)))
        {
            throw new InvalidOperationException(
                $"'{DocumentBrandingOptions.SectionName}:{nameof(DocumentBrandingOptions.Source)}' must be one of: "
                + string.Join(", ", Enum.GetNames<DocumentBrandingSourceMode>()) + ".");
        }

        services.AddOptions<DocumentBrandingOptions>().Configure(options => options.Source = source);
        services.TryAddScoped<DocumentBrandingSource>();
        return services;
    }

    /// <summary>
    /// Adds domain event dispatching services used by write-side persistence.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDomainEventProcessing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddGeneratedAuditTrailMappings();
        services.AddGeneratedIntegrationEvents();

        return services;
    }
}
