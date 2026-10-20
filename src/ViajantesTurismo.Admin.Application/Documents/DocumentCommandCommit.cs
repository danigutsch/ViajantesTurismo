using SharedKernel.Results;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Claims a prepared document command and commits its mutation and completion atomically.</summary>
/// <param name="operation">The prepared document mutation.</param>
/// <param name="documentId">The source document identifier for a rejected ownership audit.</param>
/// <param name="bookingId">The booking identifier for a rejected ownership audit.</param>
/// <param name="documentRevision">The source revision for a rejected ownership audit.</param>
/// <returns>The committed result, completed replay, or audited conflict.</returns>
public delegate Task<Result<Guid>> DocumentCommandCommit(
    Func<Task<Result<Guid>>> operation,
    Guid? documentId,
    Guid? bookingId,
    int? documentRevision);
