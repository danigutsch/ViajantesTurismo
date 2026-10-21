# Exception documentation

Owner: application maintainers. Status: active guidance. Reviewed October 20, 2026.
This manually maintained reference complements the [coding guidelines](CODING_GUIDELINES.md)
and [Result decision](adr/20251108-result-pattern-over-exceptions.md).

## Contents

- [Contract rule](#contract-rule)
- [Examples](#examples)
- [Review and tooling](#review-and-tooling)
- [Research sources](#research-sources)

## Contract rule

Document an exception with an XML `<exception cref="...">` tag when the caller can reasonably
rely on that exception type and triggering condition as part of the API contract. Describe the
condition, not the implementation line that happens to throw. A method need not contain a direct
`throw` to have a documented exception: deliberately preserved dependency failures can be contracts.

| Surface or failure | Repository expectation |
| --- | --- |
| Public method/constructor guard | Document null, empty, and range conditions that the public boundary rejects; name the parameter |
| Expected business validation | Describe the Result failure/outcome in returns/remarks; do not invent a validation exception |
| Public async operation supporting cancellation | Document `OperationCanceledException` and the supplied token condition; callers observe cancellation by awaiting the returned task |
| Stable unsupported operation | Document `NotSupportedException` and exactly which operation or mode is unsupported |
| Internal helper | Add exception docs when another component relies on a non-obvious failure contract; do not require exhaustive tags for private implementation details |
| Provider-specific adapter | Document provider failures only where the adapter intentionally exposes them as a stable, useful contract; keep them out of provider-independent Application interfaces |
| Incidental runtime/provider failure | Do not enumerate every possible exception from EF, sockets, allocation, or downstream calls as a guaranteed API contract |

Keep parameter guards consistent with nullable annotations and actual code. Do not document
`ArgumentNullException` for a nullable parameter that is accepted. HTTP Problem Details are response
contracts, not XML exception contracts. Do not expose exception messages, connection strings,
payloads, or personal data in examples or HTTP output.

## Examples

These illustrative signatures describe contract documentation, not new production abstractions.
Method names follow the repository rule that omits the Async suffix.

### Guard clauses and validation

```csharp
/// <summary>Normalizes a required business identifier.</summary>
/// <param name="identifier">The identifier to normalize.</param>
/// <returns>The normalized identifier.</returns>
/// <exception cref="ArgumentNullException"><paramref name="identifier"/> is null.</exception>
/// <exception cref="ArgumentException"><paramref name="identifier"/> is empty or whitespace.</exception>
public static string NormalizeIdentifier(string identifier)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
    return identifier.Trim().ToUpperInvariant();
}
```

For a factory returning `Result<Tour>`, document invalid dates/capacity as unsuccessful results
in `<returns>` or `<remarks>`. Do not add `<exception cref="ValidationException">` unless that
method actually promises that exception. Existing domain business rules remain in Domain.

### Async cancellation

```csharp
/// <summary>Loads an artifact from the owning store.</summary>
/// <param name="id">The artifact identity.</param>
/// <param name="cancellationToken">The token that cancels the operation.</param>
/// <returns>A task containing the artifact stream; the caller disposes the stream.</returns>
/// <exception cref="OperationCanceledException">
/// The operation is canceled through <paramref name="cancellationToken"/>.
/// </exception>
Task<Stream> OpenArtifact(Guid id, CancellationToken cancellationToken);
```

Document the abstraction's cancellation contract using `OperationCanceledException`, rather
than promising the more specific `TaskCanceledException` an implementation might produce.
State whether ownership transfers only on success. Do not promise cancellation rolls back a
commit or reverses external effects unless the implementation actually guarantees that behavior.

### Persistence and provider-specific failures

A provider-independent `Save` contract may describe atomic commit and cancellation without
promising `NpgsqlException` or `DbUpdateException`. If a concrete EF adapter intentionally exposes
an optimistic concurrency failure, its documentation can say:

```csharp
/// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">
/// The persisted revision changed before this adapter could commit the update.
/// </exception>
```

Use that tag only on the adapter contract that guarantees it. If the application translates the
failure into a conflict Result, document the conflict there instead. Incidental SQL timeout types
belong in provider diagnostics/runbooks unless they are deliberately part of the caller contract.

### Internal and unsupported operations

An internal stream helper can document `NotSupportedException` when seeking is its explicit
precondition. An upload abstraction that accepts non-seekable streams must not acquire that
precondition because its current implementation uses `Position`. For a deliberately read-only
adapter, document unsupported writes with `NotSupportedException` on the write member and explain
the read-only condition. Do not add unsupported members merely to illustrate documentation.

## Review and tooling

Review tags against guards, translated failures, cancellation propagation, and relevant tests when
changing the touched API. Use `<inheritdoc/>` where an implementation preserves its interface
contract; add concrete adapter details only where intentional. Avoid copying tags that drift.

The compiler checks XML structure/references and existing CS1591 policy covers missing public
member documentation where enabled; neither proves exception-contract completeness. Keep current
scoped suppressions in `.editorconfig`; do not enable repository-wide rules just for this guide.

An exception-specific analyzer could detect direct throws and missing tags, but transitive calls,
Result translation, and framework-discovered surfaces produce false positives. External XML-doc
analyzers add package/transitive supply-chain and suppression maintenance costs. Prefer existing
compiler checks plus review now. Any analyzer adoption requires a separate scoped proposal,
compatibility evidence, and approval; this policy adds no tool or dependency.

## Research sources

Research checked October 20, 2026. The upstream material explains XML tags and exception behavior;
the placement and review rules above are repository policy.

- [Microsoft: recommended XML documentation tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags)
- [Microsoft: exception best practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Microsoft: creating and throwing exceptions](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/exceptions/creating-and-throwing-exceptions)
