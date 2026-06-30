# String normalization guidance

## Findings

- .NET `string.Normalize()` uses Unicode normalization form C by default. Microsoft recommends
  normalizing input strings before ordinal comparisons when equivalent Unicode representations should
  compare the same.
- Normalize user-entered strings before trimming, allow-list checks, case folding, or persistence when
  values may be compared later.
- Use ordinal or ordinal-ignore-case comparison for normalized invariant keys and allow-list checks.
- Keep security-sensitive values constrained by allow-lists or narrow formats after normalization. Do
  not treat Unicode normalization as output encoding or HTML/CSS sanitization.
- Prefer canonical form C for stored display text. Use compatibility forms only when there is a clear
  domain reason to fold compatibility characters.

## Repository rule

Use `StringSanitizer` for domain and contract-adjacent string normalization before applying
case-folding or allow-list validation. Keep feature-specific validators narrow after normalization; for
example, public theme colors still require `#RRGGBB`, and font families still must match the contract
allow-list.

## References

- Microsoft Learn: `System.String.Normalize`
- Unicode Standard Annex #15: Unicode Normalization Forms
