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

Future test-analyzer enforcement is tracked separately. This ADR establishes the policy; it does not
introduce the analyzer rule itself.

## Consequences

### Positive

- Assertions name what is being checked and produce clearer failure context.
- Semantic assertion APIs make accepted alternatives visible without boolean-expression plumbing.
- A narrow analyzer rule can enforce a readable syntax signal without prescribing domain-specific APIs.

### Negative

- Some tests need a local variable or a new semantic assertion instead of a compact expression.
- Existing parenthesized `Should*` calls require incremental migration before analyzer enforcement can
  be enabled repository-wide.

## Alternatives

### Allow parenthesized assertion receivers

Rejected. The syntax commonly hides the assertion target and encourages generic boolean assertions.

### Require an analyzer immediately

Rejected. Existing tests need an intentional migration path, and choosing a semantic assertion or local
variable requires context that an automatic code fix cannot safely infer.

### Add a wrapper for every boolean expression shape

Rejected. It would create speculative assertion APIs and still fail to name application-specific
assertion targets.

## Status

Accepted.

## Links

- [Architecture decision index](../ARCHITECTURE_DECISIONS.md)
- [Test guidelines](../TEST_GUIDELINES.md)
