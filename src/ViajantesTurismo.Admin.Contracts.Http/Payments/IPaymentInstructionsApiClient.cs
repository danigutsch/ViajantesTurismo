using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides protected payment instruction generation and explicit, revision-preconditioned lifecycle operations.</summary>
public interface IPaymentInstructionsApiClient
{
    /// <summary>Lists retained immutable instruction revisions for a booking.</summary>
    Task<HttpOutcome<IReadOnlyList<GetPaymentInstructionDto>>> ListForBooking(Guid bookingId, CancellationToken ct);
    /// <summary>Gets generated financial fields and masked recipient metadata.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> GetById(Guid id, CancellationToken ct);
    /// <summary>Generates a protected immutable draft.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Generate(Guid bookingId, GeneratePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Regenerates a new immutable revision with current approved terms.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Regenerate(Guid bookingId, Guid id, GeneratePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Approve with an explicit revision precondition and stable idempotency key.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Approve(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Finalize with an explicit revision precondition and stable idempotency key.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Finalize(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes WithdrawApproval with an explicit revision precondition and stable idempotency key.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> WithdrawApproval(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Void with an explicit revision precondition and stable idempotency key.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Void(Guid id, UpdatePaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Review with an explicit revision precondition and stable idempotency key.</summary>
    Task<HttpOutcome<GetPaymentInstructionDto>> Review(Guid id, ReviewPaymentInstructionsDto input, string idempotencyKey, CancellationToken ct);
}
