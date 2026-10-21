# SharedKernel consumption

SharedKernel implementations and packaging live in the [standalone library](https://github.com/danigutsch/PandaBear.SharedKernel).
Use its [package guide](https://github.com/danigutsch/PandaBear.SharedKernel/blob/f5975b60601e245bda6bb8e45559d8ce3a24a8e0/docs/package-development.md)
for current library ownership and [application consumption](sharedkernel/development.md) for bootstrap,
pinned artifacts, validation, and rollback. This application does not build or publish SharedKernel
packages; library packaging commands and metadata belong in the standalone repository.

## Consumer responsibilities

Consume only the modules the application uses. Keep candidate versions, package hashes, local tools,
and lock files consistent with the committed candidate manifest. Validate changes through the
application bootstrap and compatibility workflow before adopting a new candidate.

Application-specific business rules, configuration, CI policy, and orchestration stay here. Reusable
library changes, test helpers, analyzers, generators, samples, and benchmarks belong in SharedKernel.
Do not restore application-owned copies of those implementations.

## Module boundary rules

These boundary rules remain relevant when proposing reusable changes in the standalone library.
Names below describe module families; published package IDs use the `PandaBear.SharedKernel.*` prefix.

`SharedKernel.<Capability>` is the primary module and core surface for that capability. Capability
families may be multi-segment, such as `SharedKernel.Messaging.IntegrationEvents`.

Primary modules must not runtime-reference descendant optional submodules through project or package
references. Optional submodules may reference the same-family primary module, a nearer parent module,
or an explicit `Abstractions` module. Optional/provider/tool suffixes are not allowed to point sideways
within the same family.

`Abstractions` modules are dependency-inversion surfaces, not implementation hosts.
Keep domain-facing and application-facing abstractions provider-neutral; provider dependencies belong
in infrastructure or provider modules.

Do not create `SharedKernel.<Capability>.Core` packages. A documented migration or compatibility
constraint must explain why the primary module cannot own that surface before adding an exception.
