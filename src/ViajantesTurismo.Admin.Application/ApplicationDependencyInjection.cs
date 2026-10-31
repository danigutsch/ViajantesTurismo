using Microsoft.Extensions.DependencyInjection;
using ViajantesTurismo.Admin.Application.Payments;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Admin.Application.Customers.Import;
using ViajantesTurismo.Admin.Application.Documents;

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
        builder.Services.AddScoped<CustomerImportCommandHandler>();
        builder.Services.AddScoped<CustomerImportConflictDetector>();
        builder.Services.AddScoped<CustomerImportWorkflowService>();
        builder.Services.AddScoped<GenerateContractDraftCommandHandler>();
        builder.Services.AddScoped<DocumentBrandingSource>();
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
        builder.Services.AddScoped<PurgeExpiredDocumentIdempotencyEntriesCommandHandler>();
        builder.Services.AddDomainEventProcessing();

        return builder;
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
