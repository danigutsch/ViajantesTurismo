# Privacy classification and redaction

ViajantesTurismo treats GDPR and Brazil LGPD as baseline privacy requirements.
The engineering rule is: collect less, store less, expose less, log less.

## Classification

- Personal data: email, phone, name, address, nationality, occupation, social profile handles,
  customer identifiers that can identify a person, and booking notes that can contain user-supplied
  personal data.
- Sensitive personal data: national ID, health or medical information, allergy information,
  biometric or physical data, and any LGPD/GDPR special-category data.
- Secrets: credentials, tokens, API keys, session identifiers, and payment secrets.
- Operational data: status codes, operation names, low-cardinality outcomes, and opaque correlation IDs.

## Logging and telemetry

- Do not log request or response bodies that may contain personal data.
- Do not log personal data in URL paths, query strings, telemetry tags, metric dimensions, or log scopes.
- Prefer source-generated `LoggerMessage` methods.
- Any log parameter that might contain personal data must be classified for redaction.
- Default redaction erases classified values before logs leave the process.

Redaction reduces accidental disclosure risk. It is not permission to collect or log more data.

## Current implementation

Shared observability enables `Microsoft.Extensions.Compliance` logging redaction with an erasing
fallback redactor. Classified source-generated logging parameters are removed from formatted log
messages before providers and OpenTelemetry exporters receive them.

## References

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/core/extensions/compliance>
- Microsoft Learn data classification: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-classification>
- Microsoft Learn data redaction: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-redaction>
- GDPR Article 5, 9, 25, and 32: <https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng>
- Brazil LGPD law: <https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm>
