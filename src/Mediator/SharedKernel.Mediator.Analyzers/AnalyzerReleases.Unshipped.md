### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
SKMED003 | Usage | Warning | Handler has invalid signature
SKMED004 | Usage | Warning | Handler is missing CancellationToken ct
SKMED005 | Usage | Warning | Handler return type does not match request response type
SKMED006 | Usage | Warning | Mediator call does not pass available CancellationToken ct
SKMED020 | Usage | Warning | Pipeline behavior has invalid generic arity
SKMED500 | Architecture | Info | Handler should not call ISender.Send
