# Assembly Fixture Guidance

Assembly fixtures are a sharing mechanism for expensive context. They are not the default answer for Admin test architecture.

## Use an assembly fixture only when

- the whole test assembly intentionally shares one expensive context
- that shared context is safe under the assembly's parallel execution model
- narrower scope choices such as local setup, class fixtures, or collection fixtures would express the lifetime incorrectly

## Do not use an assembly fixture to

- expose generic host plumbing to every test
- hide weak test boundaries
- avoid designing named lifecycle operations
- make a transitional host model look canonical

## Admin direction

- Existing assembly fixtures may remain while the repository is in transition.
- New test infrastructure should justify assembly-wide lifetime explicitly.
- Even under Aspire-managed hosting, fixture scope should be chosen deliberately rather than inherited by habit.
