# ADR-043: Domain Error Messages May Echo Domain Terms and Numbers, Not Free Text

**Status**: Accepted — 2026-10-28

## Context

The error-handling audit ([error handling and OWASP alignment](../security/error-handling-and-owasp.md)) listed one open
item under "no echo of input": `src/ViajantesTurismo.Admin.Domain/Tours/BookingErrors.cs` builds some `detail` and
`message` text by interpolating values. It has about 60 call sites. The interpolated values are:

- booking status names in transition and "cannot be modified" errors, taken from our own status enum;
- lengths and limits in note errors, which are numbers;
- the bike type and room type in `InvalidBikeType` and `InvalidRoomType`, which are the domain enums `BikeType` and
  `RoomType`.

The question was whether the bike type and room type errors reflect untrusted text.

## Findings

- The contract enums reach the domain as numbers. `src/` registers no `JsonStringEnumConverter`, and System.Text.Json
  writes and reads enums as numbers by default
  ([Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties)).
- A local probe with .NET 10 and web defaults showed that an undefined number such as `99` deserializes to the enum value
  `99` and reaches domain validation. A string such as `x<b>` fails in deserialization with a `JsonException` whose
  message does not contain the input, with or without `JsonStringEnumConverter`. The probe was a one-off script and is not
  part of the repository.
- So the text these errors can echo is a defined member name or an integer. No caller-supplied text is reflected.
- OWASP guidance targets implementation details such as stack traces, queries, keys and tokens
  ([Error Handling cheat sheet](https://cheatsheetseries.owasp.org/cheatsheets/Error_Handling_Cheat_Sheet.html), ASVS 5.0
  requirement 16.5.1), and RFC 9457 says `detail` should help the client correct the problem
  ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html)). Neither treats a domain term or a number as a leak.
- Echoing untrusted text becomes a problem when a client renders it without encoding. A number or a member name cannot
  carry markup.

## Decision

Domain errors may interpolate values that are domain enum member names, integers and limits that the domain computed or
validated. They must not interpolate caller-supplied free text, file names, identifiers taken from the request, exception
messages or any other string whose content the caller controls. The valid-value list in the bike type and room type
errors stays, because it helps the client correct the request.

`BookingErrors` is unchanged. The audit row "Validation text authored by us, no echo of input" is marked implemented
with this rule recorded as its scope.

## Consequences

- No behavior change and no contract change. Error documentation links and the roughly 60 call sites are untouched.
- A new domain error that interpolates a string the caller controls violates this rule and needs a test and review.
- If enum values are later accepted as strings, an unknown string must still fail in deserialization or be validated
  before it is placed in an error, so the rule keeps holding.

## Alternatives

- **Stop echoing the submitted enum value**: hides an integer and a domain term for no security gain, and it changes
  wording, tests and error documentation at every call site.
- **Remove every interpolation from `BookingErrors`**: most conservative, but it loses useful text such as the status
  transition names and touches the most tests.

## Links

- [Back to ADR Index](../ARCHITECTURE_DECISIONS.md)
- Related: [Error handling and OWASP alignment](../security/error-handling-and-owasp.md)
- Related: [ADR-001: Domain Validation with Factory Methods](20251108-domain-validation-factory-methods.md)
