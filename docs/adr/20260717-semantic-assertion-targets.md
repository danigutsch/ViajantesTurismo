# ADR-038: Semantic Assertion Targets

## Context

Tests communicate the behavior they verify. A parenthesized expression immediately followed by a
`Should*` assertion hides the assertion target and often collapses a domain condition into an opaque
boolean. For example, `(edge.Item2 is "OPEN" or "CLOSED").ShouldBeTrue()` does not name the
state being verified or express the accepted values through an assertion API.

The repository already prefers wrapper assertions and computed values assigned before assertion when
that improves debugging. The current status-value case needs a reusable, host-neutral assertion that
can state accepted alternatives directly.

## Decision

Maintained tests must not use a parenthesized receiver immediately followed by a `Should*` assertion.

Prefer one of these forms:

```csharp
var actualStatus = edge.Item2;
actualStatus.ShouldBeOneOf("OPEN", "CLOSED");
```

```csharp
response.StatusCode.ShouldBe(HttpStatusCode.OK);
```

Add a specialized assertion only when a current semantic assertion shape needs a small, reusable,
host-neutral API. `ShouldBeOneOf` is the current example. When no appropriate specialized assertion
exists, assign the target to a meaningfully named local before asserting it.

Analyzer enforcement is required as an immediate follow-up. It should report a parenthesized receiver
followed by `Should*`, but it must not offer a generic code fix. Choosing a specialized assertion or a
meaningfully named local requires test-specific intent.

## Consequences

### Positive

- Assertions name what is being checked and produce clearer failure context.
- Semantic assertion APIs make accepted alternatives visible without boolean-expression plumbing.
- A narrow analyzer rule can enforce a readable syntax signal without prescribing domain-specific APIs.

### Negative

- Some tests need a local variable or a new semantic assertion instead of a compact expression.
- Existing parenthesized `Should*` calls require intentional migration when analyzer enforcement lands.

## Alternatives

### Allow parenthesized assertion receivers

Rejected. The syntax commonly hides the assertion target and encourages generic boolean assertions.

### Delay analyzer enforcement

Rejected. The syntax rule should be enforced promptly so new opaque assertions do not accumulate.
Existing violations should be migrated intentionally; no generic code fix should guess whether a
specialized assertion or a named local best expresses the test contract.

### Add a wrapper for every boolean expression shape

Rejected. It would create speculative assertion APIs and still fail to name application-specific
assertion targets.

## Status

Accepted.

## Links

- [Architecture decision index](../ARCHITECTURE_DECISIONS.md)
- [Test guidelines](../TEST_GUIDELINES.md)
