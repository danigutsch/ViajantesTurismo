using PandaBear.Results;

namespace ViajantesTurismo.Admin.Application.Documents;

/// <summary>Prepares document work before invoking the durable mutation boundary.</summary>
/// <param name="commit">Acquires ownership and commits the prepared mutation.</param>
/// <returns>The prepared command result.</returns>
public delegate Task<Result<Guid>> DocumentCommandPreparation(DocumentCommandCommit commit);
